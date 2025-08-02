; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"
declare i32 @puts(i8*, ...)
@.theia_print_str = private constant[19 x i8] c"Hello from Theia!\0A\00"
declare i32 @printf(i8*, ...)
@.print_ret_fmt = private constant [16 x i8] c"%s returned %d\0A\00"


@.fn_main_str = private constant [5 x i8] c"main\00"
define i32 @main() {
entry:
  %default_a_main = alloca i8
  %default_b_main = alloca i16
  %default_c_main = alloca i16
  %default_d_main = alloca i32
  %default_e_main = alloca i32
  %default_f_main = alloca i64
  %default_g_main = alloca i64
  %default_h_main = alloca i128
  %default_i_main = alloca i256
  %default_j_main = alloca half
  %default_k_main = alloca half
  %default_l_main = alloca float
  %default_m_main = alloca float
  %default_n_main = alloca double
  %default_o_main = alloca double
  store i8 0, i8* %default_a_main
  store i16 0, i16* %default_b_main
  store i16 0, i16* %default_c_main
  store i32 0, i32* %default_d_main
  store i32 0, i32* %default_e_main
  store i64 0, i64* %default_f_main
  store i64 0, i64* %default_g_main
  store i128 0, i128* %default_h_main
  store i256 0, i256* %default_i_main
  store half 0.0, half* %default_j_main
  store half 0.0, half* %default_k_main
  store float 0.0, float* %default_l_main
  store float 0.0, float* %default_m_main
  store double 0.0, double* %default_n_main
  store double 0.0, double* %default_o_main
  %tmp0 = sub i8 0, 1
  store i8 %tmp0, i8* %default_a_main
  %tmp1 = sub i16 0, 1
  store i16 %tmp1, i16* %default_b_main
  %tmp2 = sub i16 0, 1
  store i16 %tmp2, i16* %default_c_main
  %tmp3 = sub i32 0, 1
  store i32 %tmp3, i32* %default_d_main
  %tmp4 = sub i32 0, 1
  store i32 %tmp4, i32* %default_e_main
  %tmp5 = sub i64 0, 1
  store i64 %tmp5, i64* %default_f_main
  %tmp6 = sub i64 0, 1
  store i64 %tmp6, i64* %default_g_main
  %tmp7 = sub i128 0, 1
  store i128 %tmp7, i128* %default_h_main
  %tmp8 = sub i256 0, 1
  store i256 %tmp8, i256* %default_i_main
  %tmp9 = fsub half 0.0, 1.0
  store half %tmp9, half* %default_j_main
  %tmp10 = fsub half 0.0, 1.0
  store half %tmp10, half* %default_k_main
  %tmp11 = fsub float 0.0, 1.0
  store float %tmp11, float* %default_l_main
  %tmp12 = fsub float 0.0, 1.0
  store float %tmp12, float* %default_m_main
  %tmp13 = fsub double 0.0, 1.0
  store double %tmp13, double* %default_n_main
  %tmp14 = fsub double 0.0, 1.0
  store double %tmp14, double* %default_o_main
  store i8 1, i8* %default_a_main
  store i16 1, i16* %default_b_main
  store i16 1, i16* %default_c_main
  store i32 1, i32* %default_d_main
  store i32 1, i32* %default_e_main
  store i64 1, i64* %default_f_main
  store i64 1, i64* %default_g_main
  store i128 1, i128* %default_h_main
  store i256 1, i256* %default_i_main
  store half 1.0, half* %default_j_main
  store half 1.0, half* %default_k_main
  store float 1.0, float* %default_l_main
  store float 1.0, float* %default_m_main
  store double 1.0, double* %default_n_main
  store double 1.0, double* %default_o_main
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_main_str, i32 0, i32 0), i32 0)
  ret i32 0
}

