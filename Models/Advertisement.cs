using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Lab4.Models
{
    public class Advertisement
    {
        [Required]
        public int advertisementId { get; set; }
        [Required]
        [DataType(DataType.Text)]
        public string communityID { get; set; }
        [Required]
        [DataType(DataType.Text)]
        public string fileName { get; set; }
        
        [Required]
        [DataType(DataType.Url)]
        public string url { get; set; }
    }
}
