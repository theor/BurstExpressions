using System.ComponentModel;

namespace BurstExpressions.Runtime.Runtime
{
    public class OpHideInDocumentation : System.Attribute { }
    public class OpDescriptionAttribute : System.Attribute
    {
        public string Signature { get; }
        public string Description { get; }

        public OpDescriptionAttribute(string signature, string description = null)
        {
            Signature = signature;
            Description = description;
        }
    }
    /// <summary>
    /// Each operator supported in the formula. The suffix is the arity, the number of arguments expected by the
    /// operator. Declaring the same operator with multiple arities allows overloading.
    /// </summary>
    public enum EvalOp : ushort
    {
        [OpHideInDocumentation]
        None,
        [OpHideInDocumentation]
        Const_0,
        [OpHideInDocumentation]
        Param_0,
        [OpHideInDocumentation]
        Ld_0,

        [OpDescription("x + y")]
        Add_2,
        [OpDescription("x - y")]
        Sub_2,
        [OpDescription("x * y")]
        Mul_2,
        [OpDescription("x / y")]
        Div_2,
        [OpDescription("x % y")]
        Mod_2,
        [OpDescription("pow(x,y)", "Returns x raised to the y power")]
        Pow_2,

        [OpDescription("sqrt(x)", "Returns the square root of a value")]
        Sqrt_1,
        [OpDescription("-x", "")] Minus_1,
        [OpDescription(null, "")] Abs_1,
        [OpDescription(null, "")] Saturate_1,

        [OpDescription("x(vector)", "X coordinate of the given vector")] X_1,
        [OpDescription("y(vector)", "Y coordinate of the given vector")] Y_1,
        [OpDescription("z(vector)", "Z coordinate of the given vector")] Z_1,

        [OpDescription(null, "")] Sin_1,
        [OpDescription(null, "")] Cos_1,
        [OpDescription(null, "")] Tan_1,

        [OpDescription(null, "")] CNoise_1,
        [OpDescription(null, "")] SNoise_1,
        [OpDescription(null, "")] SRDNoise_1,

        [OpDescription("fbm(pos)", "Fractal brown motion shortcut with `persistence = 1`, `octaves = 5`, `lacunarity = 0.4`")] Fbm_1,
        [OpDescription("fbm(pos, persistence, octaves, lacunarity)", "Fractal brown motion")] Fbm_4,
        [OpDescription(null, "")] Dist_2,
        [OpDescription(null, "")] SqDist_2,
        [OpDescription(null, "")] V3_3,
        [OpDescription("box(p, b)", "Projects the point `p` on a box of size `b`")] Box_2,
        [OpDescription("clamp(x, a, b)", "Clamps `x` between `a` and `b`")] Clamp_3,
        // boolean
        [OpDescription("a > b", "")] Gt_2,
        [OpDescription("a < b", "")] Lt_2,
        [OpDescription("a >= b", "")] Gte_2,
        [OpDescription("a <= b", "")] Lte_2,
        [OpDescription("select(condition, a, b)", "Return `a` if the condition is true, `b` otherwise")] Select_3,
    }
}