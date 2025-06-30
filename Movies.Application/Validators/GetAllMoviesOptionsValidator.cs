using FluentValidation;
using Movies.Application.Models;

namespace Movies.Application.Validators;


public class GetAllMoviesOptionsValidator : AbstractValidator<GetAllMoviesOptions>
{

    //to determint the only feilds that i am going to sort by wqhich is gonna prevent sql injection
    private static readonly string[] AcceptableSortFeilds =
    {
        "title" , "yearofrelease"
    };
    public GetAllMoviesOptionsValidator()
    {
        RuleFor(x => x.YearOfRelease)
        .LessThanOrEqualTo(DateTime.UtcNow.Year); //you can not serach somthing in the future

        RuleFor(x => x.SortField)
        .Must(x => x is null || AcceptableSortFeilds.Contains(x, StringComparer.OrdinalIgnoreCase))
        .WithMessage("you can only sort by 'title' or 'yearofrelease' ");

        RuleFor(x => x.PageSize)
        .InclusiveBetween(1, 25)
        .WithMessage("You Can get between 1 and 25 movies per page ");

    }

}