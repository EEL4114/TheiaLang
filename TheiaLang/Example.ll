; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"
declare i32 @puts(i8*, ...)
@.theia_print_str = private constant[19 x i8] c"Hello from Theia!\0A\00"
declare i32 @printf(i8*, ...)
@.print_ret_fmt = private constant [16 x i8] c"%s returned %d\0A\00"

%Vector3 = type { float, float, float }
%Entity = type { float, float }

@.fn_main_str = private constant [5 x i8] c"main\00"
define i32 @main() {
entry:
  %Vec4_main = alloca [4 x float]
  %tmp0 = getelementptr inbounds [4 x float], [4 x float]* %Vec4_main, i32 0, i32 1
  store float 3.0, float* %tmp0
  %z_main = alloca float
  %tmp1 = getelementptr inbounds [4 x float], [4 x float]* %Vec4_main, i32 0, i32 2
  %tmp2 = load float, float* %tmp1
  store float %tmp2, float* %z_main
  %entity_main = alloca %Entity
  %tmp3 = getelementptr %Entity, %Entity* %entity_main, i32 0, i32 0
  store float 70.0, float* %tmp3
  %tmp4 = getelementptr %Entity, %Entity* %entity_main, i32 0, i32 1
  store float 1.5, float* %tmp4

  %HP_main = alloca float
  %tmp5 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 0
  %tmp6 = load float, float* %tmp5
  store float %tmp6, float* %HP_main
  %healthPtr_main = alloca float*
  store float* %HP_main, float** %healthPtr_main
  %tmp7 = load float*, float** %healthPtr_main
  store float 5.0, float* %tmp7
  %moreHealth_main = alloca float
  %tmp8 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 0
  %tmp9 = load float, float* %tmp8
  %tmp10 = fadd float 1.0, %tmp9

  store float %tmp10, float* %moreHealth_main
  store float* %moreHealth_main, float** %healthPtr_main
  %tmp11 = load float*, float** %healthPtr_main
  %tmp12 = load float, float* %tmp11
  store float %tmp12, float* %HP_main
  %tmp13 = load float, float* %moreHealth_main
  %tmp14 = fadd float %tmp13, 1.0

  store float %tmp14, float* %moreHealth_main
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_main_str, i32 0, i32 0), i32 0)
  ret i32 0
}

