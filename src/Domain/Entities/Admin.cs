﻿using System.ComponentModel.DataAnnotations;

namespace DOJO2.Domain.Entities;

public class Admin
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string? Login { get; set; }
    [Required]

    public string? Password { get; set; }
}
