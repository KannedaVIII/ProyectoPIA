using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace InterfazWPF
{
    public class ChatMessage
    {
        public string Emisor { get; set; }
        public string Mensaje { get; set; }
        public bool EsUsuario { get; set; }
        public string InfoHerramienta { get; set; }
        public bool TieneInfoExtra => !string.IsNullOrEmpty(InfoHerramienta);
    }

    public partial class AsistenteMCP : Window
    {
        public ObservableCollection<ChatMessage> Historial { get; set; }
        private Process _servidorMcp;
        private StreamWriter _writer;

        // --- CONFIGURACIÓN LLM ---
        private const string ApiKey = "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";
        private const string UrlLLM = "https://openrouter.ai/api/v1/chat/completions";
        private const string Modelo = "meta-llama/llama-3.1-8b-instruct";

        private readonly HttpClient _httpClientLLM = new HttpClient();
        private List<object> _mensajesConversacion = new List<object>();

        public AsistenteMCP()
        {
            InitializeComponent();
            Historial = new ObservableCollection<ChatMessage>();
            listaChat.ItemsSource = Historial;
            Loaded += AsistenteMCP_Loaded;

            if (!string.IsNullOrEmpty(ApiKey) && !ApiKey.Contains("PON_AQUI"))
            {
                _httpClientLLM.DefaultRequestHeaders.Remove("Authorization");
                _httpClientLLM.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
                // Headers opcionales para OpenRouter
                _httpClientLLM.DefaultRequestHeaders.Add("HTTP-Referer", "http://localhost");
                _httpClientLLM.DefaultRequestHeaders.Add("X-Title", "WPF App");
            }
        }

        private async void AsistenteMCP_Loaded(object sender, RoutedEventArgs e)
        {
            await InicializarMCP();
        }

        private async Task InicializarMCP()
        {
            txtEstado.Text = "Lanzando Servidor MCP...";
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ServidorMCP.exe",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true, // <--- NUEVO: Capturar errores
                    CreateNoWindow = true
                };

                _servidorMcp = new Process { StartInfo = startInfo };

                // --- NUEVO: Escuchar si el servidor grita un error antes de morir ---
                _servidorMcp.ErrorDataReceived += (s, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        AgregarMensaje("CRASH SERVIDOR", args.Data, false);
                    }
                };
                // ------------------------------------------------------------------

                _servidorMcp.Start();
                _servidorMcp.BeginErrorReadLine(); // Activar la escucha de errores
                _writer = _servidorMcp.StandardInput;

                var handshake = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "initialize",
                    @params = new { protocolVersion = "2024-11-05", clientInfo = new { name = "WPF", version = "1.0" }, capabilities = new { } }
                };
                await _writer.WriteLineAsync(JsonSerializer.Serialize(handshake));

                await _servidorMcp.StandardOutput.ReadLineAsync(); // Leer respuesta handshake

                txtEstado.Text = "● Conectado (OpenRouter + MCP)";
                txtEstado.Foreground = new SolidColorBrush(Colors.SpringGreen);

                _mensajesConversacion.Add(new { role = "system", content = "Eres un asistente útil que gestiona una biblioteca de videojuegos. Usa las herramientas disponibles." });

                AgregarMensaje("IA", "¡Conectado! Pregúntame por tus juegos.", false);
            }
            catch (Exception ex)
            {
                txtEstado.Text = "Error";
                AgregarMensaje("Sistema", $"Error al iniciar ServidorMCP.exe: {ex.Message}", false);
            }
        }

        private async void btnEnviar_Click(object sender, RoutedEventArgs e) => await ProcesarMensajeUsuario();
        private async void txtMensaje_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) await ProcesarMensajeUsuario(); }

        private async Task ProcesarMensajeUsuario()
        {
            string texto = txtMensaje.Text.Trim();
            if (string.IsNullOrEmpty(texto)) return;

            AgregarMensaje("Tú", texto, true);
            txtMensaje.Clear();
            ScrollAlFinal();

            _mensajesConversacion.Add(new { role = "user", content = texto });

            await ConsultarLLM();
        }

        private async Task ConsultarLLM()
        {
            txtEstado.Text = "Pensando...";
            try
            {
                var toolsDefinicion = new object[]
                {
                    new {
                        type = "function",
                        function = new {
                            name = "contar_juegos",
                            description = "Devuelve la cantidad total de juegos.",
                            parameters = new { type = "object", properties = new { } }
                        }
                    },
                    new {
                        type = "function",
                        function = new {
                            name = "buscar_juego",
                            description = "Busca juegos por nombre.",
                            parameters = new {
                                type = "object",
                                properties = new { nombre = new { type = "string", description = "Nombre del juego" } },
                                required = new[] { "nombre" }
                            }
                        }
                    }
                };

                var requestBody = new
                {
                    model = Modelo,
                    messages = _mensajesConversacion,
                    tools = toolsDefinicion,
                    tool_choice = "auto"
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClientLLM.PostAsync(UrlLLM, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    AgregarMensaje("Error API", $"Status: {response.StatusCode} - {errorText}", false);
                    return;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                if (root.TryGetProperty("error", out var errorElem))
                {
                    AgregarMensaje("Error OpenAI", errorElem.GetProperty("message").GetString(), false);
                    return;
                }

                var choice = root.GetProperty("choices")[0];
                var message = choice.GetProperty("message");
                var finishReason = choice.GetProperty("finish_reason").GetString();

                if (finishReason == "tool_calls")
                {
                    var toolCalls = message.GetProperty("tool_calls");

                    // --- CORRECCIÓN IMPORTANTE: CLONE() PARA EVITAR ERROR 'DISPOSED' ---
                    _mensajesConversacion.Add(message.Clone());
                    // ------------------------------------------------------------------

                    foreach (var toolCall in toolCalls.EnumerateArray())
                    {
                        string toolCallId = toolCall.GetProperty("id").GetString();
                        var function = toolCall.GetProperty("function");
                        string nombreTool = function.GetProperty("name").GetString();
                        string argumentos = function.GetProperty("arguments").GetString();

                        AgregarMensaje("Sistema", $"Ejecutando: {nombreTool}", false, $"Args: {argumentos}");

                        string resultadoMcp = await EjecutarToolEnMCP(nombreTool, argumentos);

                        _mensajesConversacion.Add(new
                        {
                            role = "tool",
                            tool_call_id = toolCallId,
                            name = nombreTool,
                            content = resultadoMcp
                        });
                    }
                    await ConsultarLLM();
                }
                else
                {
                    string contenido = message.GetProperty("content").GetString();
                    if (!string.IsNullOrEmpty(contenido))
                    {
                        AgregarMensaje("IA", contenido, false);
                        _mensajesConversacion.Add(new { role = "assistant", content = contenido });
                    }
                }
            }
            catch (Exception ex)
            {
                AgregarMensaje("Error", $"Excepción General: {ex.Message}", false);
            }
            finally
            {
                txtEstado.Text = "● Conectado";
                ScrollAlFinal();
            }
        }

        private async Task<string> EjecutarToolEnMCP(string nombreTool, string jsonArgumentos)
        {
            try
            {
                if (_servidorMcp == null || _servidorMcp.HasExited)
                    return "Error: El servidor MCP se ha cerrado inesperadamente.";

                var mcpRequest = new
                {
                    jsonrpc = "2.0",
                    id = DateTime.Now.Ticks,
                    method = "tools/call",
                    @params = new
                    {
                        name = nombreTool,
                        arguments = JsonSerializer.Deserialize<object>(jsonArgumentos)
                    }
                };

                await _writer.WriteLineAsync(JsonSerializer.Serialize(mcpRequest));

                // Leemos la respuesta. Si el servidor crashea aquí, saltará la excepción o devolverá null
                string respuestaJson = await _servidorMcp.StandardOutput.ReadLineAsync();

                if (string.IsNullOrEmpty(respuestaJson)) return "Error: Respuesta vacía (posible crash del servidor)";

                using var doc = JsonDocument.Parse(respuestaJson);
                if (doc.RootElement.TryGetProperty("result", out var result))
                {
                    var content = result.GetProperty("content")[0];
                    return content.GetProperty("text").GetString();
                }
                if (doc.RootElement.TryGetProperty("error", out var errorMcp))
                {
                    return $"Error MCP: {errorMcp.GetProperty("message").GetString()}";
                }
                return "Error desconocido en respuesta MCP";
            }
            catch (Exception ex)
            {
                return $"Excepción al llamar a MCP: {ex.Message}";
            }
        }

        private void AgregarMensaje(string emisor, string mensaje, bool esUsuario, string infoTool = null)
        {
            Dispatcher.Invoke(() => {
                Historial.Add(new ChatMessage { Emisor = emisor, Mensaje = mensaje, EsUsuario = esUsuario, InfoHerramienta = infoTool });
            });
        }

        private void ScrollAlFinal()
        {
            Dispatcher.Invoke(() => {
                if (listaChat.Items.Count > 0) scrollChat.ScrollToBottom();
            });
        }
    }
}