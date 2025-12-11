using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace InterfazMAUI
{
    public partial class AsistenteIA : ContentPage
    {
        public ObservableCollection<ChatMessage> Historial { get; set; }
        private Process _servidorMcp;
        private StreamWriter _writer;

        // --- CONFIGURACIÓN LLM ---
        private const string ApiKey = "MI APY KEY"; // TU KEY
        private const string UrlLLM = "https://openrouter.ai/api/v1/chat/completions";
        private const string Modelo = "meta-llama/llama-3.1-8b-instruct";

        private readonly HttpClient _httpClientLLM = new HttpClient();
        private List<object> _mensajesConversacion = new List<object>();

        public AsistenteIA()
        {
            InitializeComponent();
            Historial = new ObservableCollection<ChatMessage>();
            listaChat.ItemsSource = Historial;

            // Configurar Headers HTTP
            if (!string.IsNullOrEmpty(ApiKey) && !ApiKey.Contains("PON_AQUI"))
            {
                _httpClientLLM.DefaultRequestHeaders.Remove("Authorization");
                _httpClientLLM.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
                _httpClientLLM.DefaultRequestHeaders.Add("HTTP-Referer", "http://localhost");
                _httpClientLLM.DefaultRequestHeaders.Add("X-Title", "MAUI App");
            }

            // En MAUI usamos el evento Loaded de la página
            this.Loaded += AsistenteIA_Loaded;
        }

        private async void AsistenteIA_Loaded(object sender, EventArgs e)
        {
            await InicializarMCP();
        }

        private async Task InicializarMCP()
        {
            // NOTA IMPORTANTE: Process.Start solo funciona en MAUI para WINDOWS o MAC (Desktop).
            MainThread.BeginInvokeOnMainThread(() => txtEstado.Text = "Lanzando Servidor MCP...");

            try
            {
                // --- CORRECCIÓN DE LA RUTA (SOLUCIÓN SYSTEM32) ---

                // 1. Obtenemos la carpeta real donde se está ejecutando la app (net8.0-windows...)
                string directorioBase = AppDomain.CurrentDomain.BaseDirectory;

                // 2. Construimos la ruta absoluta al ejecutable
                string exePath = Path.Combine(directorioBase, "ServidorMCP.exe");

                // 3. Verificación de seguridad (útil para depurar)
                if (!File.Exists(exePath))
                {
                    AgregarMensaje("SISTEMA", $"⚠️ No encuentro el archivo en:\n{exePath}\n\nAsegúrate de copiar ServidorMCP.exe en la carpeta net8.0-windows... dentro de bin/Debug", false);
                    MainThread.BeginInvokeOnMainThread(() => txtEstado.Text = "Error: Falta .exe");
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    // 4. IMPORTANTE: Forzamos el directorio de trabajo a la carpeta de la app
                    WorkingDirectory = directorioBase,

                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                _servidorMcp = new Process { StartInfo = startInfo };

                _servidorMcp.ErrorDataReceived += (s, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        AgregarMensaje("CRASH SERVIDOR", args.Data, false);
                    }
                };

                _servidorMcp.Start();
                _servidorMcp.BeginErrorReadLine();
                _writer = _servidorMcp.StandardInput;

                var handshake = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "initialize",
                    @params = new { protocolVersion = "2024-11-05", clientInfo = new { name = "MAUI Client", version = "1.0" }, capabilities = new { } }
                };

                await _writer.WriteLineAsync(JsonSerializer.Serialize(handshake));
                await _servidorMcp.StandardOutput.ReadLineAsync(); // Leer respuesta handshake

                MainThread.BeginInvokeOnMainThread(() => {
                    txtEstado.Text = "● Conectado (OpenRouter + MCP)";
                    txtEstado.TextColor = Colors.Green;
                });

                _mensajesConversacion.Add(new { role = "system", content = "Eres un asistente útil que gestiona una biblioteca de videojuegos. Usa las herramientas disponibles." });

                AgregarMensaje("IA", "¡Conectado! Pregúntame por tus juegos.", false);
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() => txtEstado.Text = "Error de conexión");
                AgregarMensaje("Sistema", $"Error al iniciar ServidorMCP.exe. Error: {ex.Message}", false);
            }
        }

        // Evento Click del Botón
        private async void BtnEnviar_Clicked(object sender, EventArgs e) => await ProcesarMensajeUsuario();

        // Evento 'Completed' del Editor (Enter)
        private async void TxtMensaje_Completed(object sender, EventArgs e) => await ProcesarMensajeUsuario();

        private async Task ProcesarMensajeUsuario()
        {
            string texto = txtMensaje.Text?.Trim();
            if (string.IsNullOrEmpty(texto)) return;

            AgregarMensaje("Tú", texto, true);
            txtMensaje.Text = string.Empty;

            // Ocultar teclado en móviles (opcional)
            // txtMensaje.Unfocus(); 

            _mensajesConversacion.Add(new { role = "user", content = texto });

            await ConsultarLLM();
        }

        private async Task ConsultarLLM()
        {
            MainThread.BeginInvokeOnMainThread(() => txtEstado.Text = "Pensando...");

            try
            {
                // Definición de Tools (Igual que en WPF)
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
                    _mensajesConversacion.Add(message.Clone());

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
                    // Recursividad para que la LLM responda con la info de la tool
                    await ConsultarLLM();
                }
                else
                {
                    string contenido = "";
                    if (message.TryGetProperty("content", out var contentElem) && contentElem.ValueKind != JsonValueKind.Null)
                    {
                        contenido = contentElem.GetString();
                    }

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
                MainThread.BeginInvokeOnMainThread(() => {
                    txtEstado.Text = "● Conectado";
                    ScrollAlFinal();
                });
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
                string respuestaJson = await _servidorMcp.StandardOutput.ReadLineAsync();

                if (string.IsNullOrEmpty(respuestaJson)) return "Error: Respuesta vacía (posible crash del servidor)";

                using var doc = JsonDocument.Parse(respuestaJson);
                if (doc.RootElement.TryGetProperty("result", out var result))
                {
                    var contentArray = result.GetProperty("content");
                    if (contentArray.GetArrayLength() > 0)
                        return contentArray[0].GetProperty("text").GetString();
                    return "Sin contenido";
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
            // En MAUI usamos MainThread para tocar la UI
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Historial.Add(new ChatMessage
                {
                    Emisor = emisor,
                    Mensaje = mensaje,
                    EsUsuario = esUsuario,
                    InfoHerramienta = infoTool
                });

                ScrollAlFinal();
            });
        }

        private void ScrollAlFinal()
        {
            if (Historial.Count > 0)
            {
                // Scroll suave al último elemento
                listaChat.ScrollTo(Historial.Last(), position: ScrollToPosition.End, animate: true);
            }
        }
    }
}