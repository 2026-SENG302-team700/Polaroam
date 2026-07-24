using System.ComponentModel.DataAnnotations;
using SENG302.Api.Models.Entities;

namespace SENG302.Api.Models.Interfaces;

public interface IPostRequest {
    public  string Title { get; set; }
    public  string Content { get; set; }
    public  DateTime Date { get; set; }
    public string Location { get; set; }
}