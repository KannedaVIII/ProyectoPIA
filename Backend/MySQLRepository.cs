using MySql.Data.MySqlClient;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System;

namespace Backend
{
    public class MySQLRepository : IRepository
    {
        private readonly string _connectionString;

        public MySQLRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CadenaMySQL");
        }

        public List<Videojuegos> GetAll()
        {
            var lista = new List<Videojuegos>();

            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                // Tabla: videojuegos
                string query = "SELECT * FROM videojuegos";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(MapearJuego(reader));
                        }
                    }
                }
            }
            return lista;
        }

        public Videojuegos GetByTitle(string title)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                // Buscamos por la columna 'title'
                string query = "SELECT * FROM videojuegos WHERE title = @title";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@title", title);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapearJuego(reader);
                        }
                    }
                }
            }
            return null;
        }

        public void Insert(Videojuegos juego)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                // Usamos las columnas EXACTAS de tu imagen
                string query = @"INSERT INTO videojuegos 
                                 (title, releaseDate, team, rating, timesListed, numberReviews, genres, summary, review) 
                                 VALUES 
                                 (@title, @releaseDate, @team, @rating, @timesListed, @numberReviews, @genres, @summary, @review)";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    AsignarParametros(cmd, juego);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(Videojuegos juego)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"UPDATE videojuegos SET 
                                 releaseDate=@releaseDate, team=@team, rating=@rating, 
                                 timesListed=@timesListed, numberReviews=@numberReviews,
                                 genres=@genres, summary=@summary, review=@review 
                                 WHERE title = @title";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    AsignarParametros(cmd, juego);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(Videojuegos juego)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = "DELETE FROM videojuegos WHERE title = @title";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@title", juego.Title);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void CargarDesdeLista(List<Videojuegos> lista)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                foreach (var juego in lista)
                {
                    // Verificamos duplicados por 'title'
                    string check = "SELECT COUNT(*) FROM videojuegos WHERE title = @title";
                    using (var cmdCheck = new MySqlCommand(check, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@title", juego.Title);
                        long count = (long)cmdCheck.ExecuteScalar();
                        if (count > 0) continue;
                    }

                    Insert(juego);
                }
            }
        }

        // --- MÉTODOS AUXILIARES ---

        private void AsignarParametros(MySqlCommand cmd, Videojuegos juego)
        {
            // Asignamos tus propiedades C# a los parámetros SQL
            cmd.Parameters.AddWithValue("@title", juego.Title);
            cmd.Parameters.AddWithValue("@releaseDate", juego.ReleaseDate);
            cmd.Parameters.AddWithValue("@team", juego.Team);
            cmd.Parameters.AddWithValue("@rating", juego.Rating);

            // Si tu objeto C# tiene estas propiedades, las usamos. Si no, ponemos un valor por defecto.
            // Asumo que tu clase Videojuegos tiene estas propiedades como string:
            cmd.Parameters.AddWithValue("@timesListed", juego.TimesListed ?? "0");
            cmd.Parameters.AddWithValue("@numberReviews", juego.NumberReviews ?? "0");

            cmd.Parameters.AddWithValue("@genres", juego.Genres);
            cmd.Parameters.AddWithValue("@summary", juego.Summary ?? "");
            cmd.Parameters.AddWithValue("@review", juego.Review ?? "");
        }

        private Videojuegos MapearJuego(MySqlDataReader reader)
        {
            // LEEMOS USANDO LOS NOMBRES EXACTOS DE TU IMAGEN
            return new Videojuegos(
                title: reader["title"].ToString(),
                releaseDate: reader["releaseDate"].ToString(),
                team: reader["team"].ToString(),
                rating: Convert.ToDecimal(reader["rating"]),
                timesListed: reader["timesListed"].ToString(),
                numberReviews: reader["numberReviews"].ToString(),
                genres: reader["genres"].ToString(),
                summary: reader["summary"].ToString(),
                review: reader["review"].ToString()
            );
        }
    }
}