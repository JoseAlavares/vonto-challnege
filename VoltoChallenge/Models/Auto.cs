using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoltoChallenge.Models
{
    [Table("auto")]
    [Serializable]
    public class Auto
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key, Column("id")]
        public int id { get; set; }
        [Column("placa")]
        public string? placa { get; set; }
        [Column("total_minutos")]
        public int? total_minutos { get; set; }
        [Column("id_tipo_cliente")]
        public int id_tipo_cliente { get; set; }
    }
}