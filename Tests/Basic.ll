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
  store i8 -1, i8* %default_a_main
  store i16 -1, i16* %default_b_main
  store i16 -1, i16* %default_c_main
  store i32 -1, i32* %default_d_main
  store i32 -1, i32* %default_e_main
  store i64 -1, i64* %default_f_main
  store i64 -1, i64* %default_g_main
  store i128 -1, i128* %default_h_main
  store i256 -1, i256* %default_i_main
  store half -1.0, half* %default_j_main
  store half -1.0, half* %default_k_main
  store float -1.0, float* %default_l_main
  store float -1.0, float* %default_m_main
  store double -1.0, double* %default_n_main
  store double -1.0, double* %default_o_main
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

