namespace Eval.Runtime
{
    /// <summary>
    /// Each operator supported in the formula. The suffix is the arity, the number of arguments expected by the
    /// operator. Declaring the same operator with multiple arities allows overloading.
    /// </summary>
    public enum EvalOp
    {
        None,
        Const_0,
        Param_0,
        Ld_0,
        Add_2, Sub_2,
        Mul_2, Div_2,
        Mod_2,
        Pow_2,
        Minus_1,
        Abs_1,
        Saturate_1,
        X_1, Y_1, Z_1,
        Sin_1, Cos_1, Tan_1,
        CNoise_1,
        SNoise_1,
        SRDNoise_1,
        Fbm_1,
        Fbm_4,
        Dist_2,
        SqDist_2,
        V3_3,
        Box_2,
    }
}