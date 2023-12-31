﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nxPinterest.Data.Models
{
    public class UserMediaTags
    {
        [Key]
        public int UserMediaTagsId { get; set; }
        public string UserMediaName { get; set; }
        public int ContainerId { get; set; }
        public byte TagsType { get; set; }
        public string Tag { get; set; }
        public double Confidence { get; set; }
        public DateTime Created { get; set; }
    }
}
