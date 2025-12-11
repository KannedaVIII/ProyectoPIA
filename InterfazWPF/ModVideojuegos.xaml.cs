using System;
using System.Windows;
using Backend;

namespace InterfazWPF
{
    public partial class ModVideojuegos : Window
    {
        private Videojuegos _juegoOriginal;

        public ModVideojuegos(Videojuegos juegoAEditar)
        {
            InitializeComponent();
            _juegoOriginal = juegoAEditar;
            CargarDatos();
        }

        private void CargarDatos()
        {
            txtTitulo.Text = _juegoOriginal.Title;
            txtGenero.Text = _juegoOriginal.Genres;
            txtFecha.Text = _juegoOriginal.ReleaseDate;
            txtEquipo.Text = _juegoOriginal.Team;
            txtRating.Text = _juegoOriginal.Rating.ToString();

            // --- CAMBIO IMPORTANTE ---
            // Bloqueamos el título. Como tu base de datos usa el Título para buscar (WHERE title = ...),
            // no podemos permitir que se cambie aquí o la actualización fallará.
            txtTitulo.IsEnabled = false;
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(txtEquipo.Text))
            {
                MessageBox.Show("El equipo no puede estar vacío.", "Aviso");
                return;
            }

            // --- ACTUALIZACIÓN ---
            // NO actualizamos _juegoOriginal.Title (porque es la clave)

            _juegoOriginal.Genres = txtGenero.Text;
            _juegoOriginal.ReleaseDate = txtFecha.Text;
            _juegoOriginal.Team = txtEquipo.Text;

            // Parsear rating
            if (decimal.TryParse(txtRating.Text.Replace(".", ","), out decimal nuevoRating))
            {
                _juegoOriginal.Rating = nuevoRating;
            }
            else
            {
                // Opcional: Si escriben mal el número, lo dejamos como estaba o ponemos 0
                // _juegoOriginal.Rating = 0; 
            }

            // Al poner DialogResult = true, la MainWindow sabrá que debe enviar 
            // este objeto '_juegoOriginal' (ya modificado en memoria) a la API.
            DialogResult = true;
            this.Close();
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}