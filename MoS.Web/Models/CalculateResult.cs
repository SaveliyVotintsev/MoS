using System.Numerics;

namespace MoS.Web.Models;

public record CalculateResult(Complex[] Roots, Complex[] Derivatives, Complex[] HBezEList, string[] Eh, string Result);
