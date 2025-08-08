; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"


define i32 @main() {
entry:
  %a_main = alloca i1
  %b_main = alloca i1
  store i1 1, i1* %b_main
  %c_main = alloca i1
  %tmp0 = load i1, i1* %a_main
  %tmp1 = load i1, i1* %b_main
  %tmp2 = or i1 %tmp0, %tmp1

  store i1 %tmp2, i1* %c_main
  %d_main = alloca i1
  %tmp3 = load i1, i1* %a_main
  %tmp4 = load i1, i1* %b_main
  %tmp5 = and i1 %tmp3, %tmp4

  store i1 %tmp5, i1* %d_main
  %i_main = alloca i32
  store i32 1, i32* %i_main
  %j_main = alloca i32
  store i32 3, i32* %j_main
  %k_main = alloca i32
  %tmp6 = load i32, i32* %i_main
  %tmp7 = load i32, i32* %j_main
  %tmp8 = sdiv i32 %tmp6, %tmp7

  store i32 %tmp8, i32* %k_main
  %f_main = alloca float
  store float 1.0, float* %f_main
  %g_main = alloca float
  store float 3.0, float* %g_main
  %h_main = alloca float
  %tmp9 = load float, float* %f_main
  %tmp10 = load float, float* %g_main
  %tmp11 = fdiv float %tmp9, %tmp10

  store float %tmp11, float* %h_main
  %tmp12 = load i32, i32* %k_main
  ret i32 %tmp12
}

