namespace InterfazMAUI
{
    public class ChatMessage
    {
        public string Emisor { get; set; }
        public string Mensaje { get; set; }
        public bool EsUsuario { get; set; } // True = Derecha (Verde), False = Izquierda (Gris)
        public string InfoHerramienta { get; set; }
        public bool TieneInfoExtra => !string.IsNullOrEmpty(InfoHerramienta);

        // Propiedades de ayuda para la UI de MAUI
        public LayoutOptions Alineacion => EsUsuario ? LayoutOptions.End : LayoutOptions.Start;
        public Color ColorFondo => EsUsuario ? Color.FromArgb("#DCF8C6") : Color.FromArgb("#EAEAEA"); // Verde WhatsApp vs Gris
        public Color ColorTexto => Colors.Black;
    }
}