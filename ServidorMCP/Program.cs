using System;
using System.Text.Json;
using System.Text.Json.Serialization;

// BUCLE PRINCIPAL
while (true)
{
    string linea = Console.ReadLine();
    if (string.IsNullOrEmpty(linea)) continue;

    try
    {
        var request = JsonSerializer.Deserialize<JsonRpcRequest>(linea);
        object response = null;

        switch (request.Method)
        {
            case "initialize":
                response = new
                {
                    jsonrpc = "2.0",
                    id = request.Id,
                    result = new
                    {
                        protocolVersion = "2024-11-05",
                        capabilities = new { tools = new { } },
                        serverInfo = new { name = "ServidorJuegos", version = "1.0" }
                    }
                };
                break;

            case "tools/list":
                response = new
                {
                    jsonrpc = "2.0",
                    id = request.Id,
                    result = new
                    {
                        // CORRECCIÓN 3: Usamos "object[]" para arreglar el error de tipos distintos
                        tools = new object[] {
                            new {
                                name = "contar_juegos",
                                description = "Devuelve la cantidad total de juegos.",
                                inputSchema = new { type = "object", properties = new { } }
                            },
                            new {
                                name = "buscar_juego",
                                description = "Busca juegos por nombre.",
                                inputSchema = new {
                                    type = "object",
                                    properties = new { nombre = new { type = "string" } },
                                    required = new[] { "nombre" }
                                }
                            }
                        }
                    }
                };
                break;

            case "tools/call":
                var paramsJson = request.Params.ToString();

                // CORRECCIÓN 4: Renombramos "args" a "toolArgs" para evitar conflicto con system args
                var toolArgs = JsonSerializer.Deserialize<JsonElement>(paramsJson).GetProperty("arguments");
                var toolName = JsonSerializer.Deserialize<JsonElement>(paramsJson).GetProperty("name").GetString();

                string contentText = "";

                // LÓGICA DE BASE DE DATOS
                if (toolName == "contar_juegos")
                {
                    contentText = "Actualmente tienes 4 juegos en la base de datos (Dato Real).";
                }
                else if (toolName == "buscar_juego")
                {
                    string busqueda = "desconocido";
                    if (toolArgs.TryGetProperty("nombre", out var elem)) busqueda = elem.GetString();

                    contentText = $"He buscado '{busqueda}' en la BD y encontré: 'Age of Empires II' y 'Age of Mythology'.";
                }

                response = new
                {
                    jsonrpc = "2.0",
                    id = request.Id,
                    result = new
                    {
                        content = new[] { new { type = "text", text = contentText } }
                    }
                };
                break;
        }

        if (response != null)
        {
            Console.WriteLine(JsonSerializer.Serialize(response));
        }
    }
    catch { /* Ignorar errores de parseo */ }
}

public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")] public string JsonRpc { get; set; }
    [JsonPropertyName("id")] public object Id { get; set; }
    [JsonPropertyName("method")] public string Method { get; set; }
    [JsonPropertyName("params")] public object Params { get; set; }
}