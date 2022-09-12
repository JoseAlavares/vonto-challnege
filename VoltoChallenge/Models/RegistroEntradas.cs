using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoltoChallenge.Models
{
    [Table("registroentradas")]
    [Serializable]
    public class RegistroEntradas
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key, Column("id")]
        public int id { get; set; }
        [Column("id_auto")]
        public int id_auto { get; set; }
        [Column("entrada")]
        public DateTime? entrada { get; set; }
        [Column("salida")]
        public DateTime? salida{ get; set; }
        [Column("activo")]
        public bool activo { get; set; }
    }
}