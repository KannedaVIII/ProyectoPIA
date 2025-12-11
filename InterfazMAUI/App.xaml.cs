namespace InterfazMAUI
{
    public partial class App : Application
    {
        public static string UsuarioActual { get; set; }
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new Register());
        }
    }
}
