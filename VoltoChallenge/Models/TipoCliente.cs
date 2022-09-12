using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoltoChallenge.Models
{
    [Table("tipocliente")]
    [Serializable]
    public class TipoCliente
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key, Column("id")]
        public int id { get; set; }
        [Column("nombre")]
        public string? nombre { get; set; }
        [Column("tarifa")]
        public float tarifa { get; set; }
    }
}