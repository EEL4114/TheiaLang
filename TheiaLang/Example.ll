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
  %entity_main = alloca %Entity
  %tmp0 = getelementptr %Entity, %Entity* %entity_main, i32 0, i32 0
  store float 70.0, float* %tmp0
  %tmp1 = getelementptr %Entity, %Entity* %entity_main, i32 0, i32 1
  store float 1.5, float* %tmp1

  %HP_main = alloca float
  %tmp2 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 0
  %tmp3 = load float, float* %tmp2
  store float %tmp3, float* %HP_main
  %healthPtr_main = alloca float*
  store float* %HP_main, float** %healthPtr_main
  %tmp4 = load float*, float** %healthPtr_main
  store float 5.0, float* %tmp4
  %moreHealth_main = alloca float
  %tmp5 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 0
  %tmp6 = load float, float* %tmp5
  %tmp7 = fadd float 1.0, %tmp6

  store float %tmp7, float* %moreHealth_main
  store float* %moreHealth_main, float** %healthPtr_main
  %tmp8 = load float*, float** %healthPtr_main
  %tmp9 = load float, float* %tmp8
  store float %tmp9, float* %HP_main
  %tmp10 = load float, float* %moreHealth_main
  %tmp11 = fadd float %tmp10, 1.0

  store float %tmp11, float* %moreHealth_main
  %tmp12 = call i32 @m(i32 7)
  %tmp13 = call i1 @n(i1 1)
  %tmp14 = call i1 @n(i1 0)
  %t_main = alloca i32
  %tmp15 = call i32 @m(i32 8)
  store i32 %tmp15, i32* %t_main
  %u_main = alloca i32
  %tmp16 = call i32 @m(i32 8)
  %tmp17 = add i32 1, %tmp16

  %tmp18 = add i32 %tmp17, 9

  store i32 %tmp18, i32* %u_main
  %defaultInt_main = alloca i32
  %defaultFloat_main = alloca float
  %defaultBool_main = alloca i1
  %tmp19 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 0
  store float 4.0, float* %tmp19
  %default_s32_main = alloca i32
  %default_f32_main = alloca float
  %default_bool_main = alloca i1
  %quad_main = alloca fp128
  store fp128 0xL1C3, fp128* %quad_main
  %i_main = alloca i32
  %tmp20 = sub i32 0, 1
  store i32 %tmp20, i32* %i_main
  %tmp21 = load i32, i32* %i_main
  %tmp22 = call i32 @Abs(i32 %tmp21)
  %tmp23 = load i32, i32* %i_main
  %tmp24 = sub i32 0, %tmp23
  %tmp25 = call i32 @Abs(i32 %tmp24)
  %j_main = alloca float
  %tmp26 = fsub float 0.0, 1.0
  store float %tmp26, float* %j_main
  %fg_main = alloca i1
  store i1 0, i1* %fg_main
  %ffg_main = alloca i1
  store i1 1, i1* %ffg_main
  %a_main = alloca i32
  store i32 5, i32* %a_main
  %b_main = alloca i32
  store i32 10, i32* %b_main
  %c_main = alloca i32
  %tmp27 = load i32, i32* %a_main
  %tmp28 = load i32, i32* %b_main
  %tmp29 = add i32 %tmp27, %tmp28

  store i32 %tmp29, i32* %c_main
  %d_main = alloca i32
  store i32 4, i32* %d_main
  %tmp30 = load i32, i32* %a_main
  %tmp31 = load i32, i32* %c_main
  %tmp32 = add i32 %tmp30, %tmp31

  store i32 %tmp32, i32* %d_main
  %tmp33 = load i32, i32* %d_main
  %tmp34 = add i32 %tmp33, 42

  store i32 %tmp34, i32* %d_main
  %f_main = alloca float
  store float 2.5, float* %f_main
  %tmp35 = load float, float* %f_main
  %tmp36 = fmul float %tmp35, 2.0

  store float %tmp36, float* %f_main
  %g_main = alloca float
  store float 3.0, float* %g_main
  %h_main = alloca float
  %tmp37 = load float, float* %f_main
  %tmp38 = load float, float* %g_main
  %tmp39 = fsub float %tmp37, %tmp38

  store float %tmp39, float* %h_main
  %ok_main = alloca i1
  %tmp40 = load i32, i32* %c_main
  %tmp41 = icmp sgt i32 %tmp40, 5

  store i1 %tmp41, i1* %ok_main
  %tmp42 = load i32, i32* %c_main
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_main_str, i32 0, i32 0), i32 %tmp42)
  ret i32 %tmp42
}

@.fn_Branchy_str = private constant [8 x i8] c"Branchy\00"
define i32 @Branchy(i32 %value) {
entry:
  %tmp43 = alloca i32
  store i32 %value, i32* %tmp43
  %tmp44 = load i32, i32* %tmp43
  %tmp45 = icmp eq i32 %tmp44, 0

  br i1 %tmp45, label %if_then_0, label %if_else_0
if_then_0:
  store i32 42, i32* %tmp43
  %f_if_then256 = alloca float
  store float 0.0, float* %f_if_then256
  %tmp46 = fsub float 0.0, 1.0
  store float %tmp46, float* %f_if_then256
  br label %if_end_0
if_else_0:
  %tmp47 = sub i32 0, 42
  store i32 %tmp47, i32* %tmp43
  %f_if_else272 = alloca float
  store float 78.0, float* %f_if_else272
  br label %if_end_0
if_end_0:
  %tmp48 = load i32, i32* %tmp43
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Branchy_str, i32 0, i32 0), i32 %tmp48)
  ret i32 %tmp48
}

@.fn_Abs_str = private constant [4 x i8] c"Abs\00"
define i32 @Abs(i32 %value) {
entry:
  %tmp49 = alloca i32
  store i32 %value, i32* %tmp49
  %tmp50 = load i32, i32* %tmp49
  %tmp51 = icmp slt i32 %tmp50, 0

  br i1 %tmp51, label %if_then_1, label %if_end_1
if_then_1:
  %tmp52 = load i32, i32* %tmp49
  %tmp53 = sub i32 0, %tmp52
  ret i32 %tmp53
  br label %if_end_1
if_end_1:
  %tmp54 = load i32, i32* %tmp49
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Abs_str, i32 0, i32 0), i32 %tmp54)
  ret i32 %tmp54
}

@.fn_Loopy_str = private constant [6 x i8] c"Loopy\00"
define i32 @Loopy(i32 %value) {
entry:
  %tmp55 = alloca i32
  store i32 %value, i32* %tmp55
  %i_Loopy = alloca i32
  store i32 0, i32* %i_Loopy
  br label %for_cond2
for_cond2:
  %tmp56 = load i32, i32* %i_Loopy
  %tmp57 = load i32, i32* %tmp55
  %tmp58 = icmp slt i32 %tmp56, %tmp57

  br i1 %tmp58, label %for_body2, label %for_end2
for_body2:
  %tmp59 = load i32, i32* %tmp55
  %tmp60 = load i32, i32* %i_Loopy
  %tmp61 = add i32 %tmp59, %tmp60

  store i32 %tmp61, i32* %tmp55
  br label %for_iter2
for_iter2:
  %tmp62 = load i32, i32* %i_Loopy
  %tmp63 = sub i32 %tmp62, 3

  store i32 %tmp63, i32* %i_Loopy
  br label %for_cond2
for_end2:
  %tmp64 = load i32, i32* %tmp55
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Loopy_str, i32 0, i32 0), i32 %tmp64)
  ret i32 %tmp64
}

@.fn_n_str = private constant [2 x i8] c"n\00"
define i1 @n(i1 %b) {
entry:
  %tmp65 = alloca i1
  store i1 %b, i1* %tmp65
  %tmp66 = load i1, i1* %tmp65
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([3 x i8], [3 x i8]* @.fn_n_str, i32 0, i32 0), i1 %tmp66)
  ret i1 %tmp66
}

@.fn_m_str = private constant [2 x i8] c"m\00"
define i32 @m(i32 %j) {
entry:
  %tmp67 = alloca i32
  store i32 %j, i32* %tmp67
  %i_m = alloca i32
  store i32 3, i32* %i_m
  %tmp68 = load i32, i32* %tmp67
  %tmp69 = add i32 %tmp68, 4

  store i32 %tmp69, i32* %tmp67
  %health_m = alloca i32
  store i32 7, i32* %health_m
  %tmp70 = load i32, i32* %tmp67
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_m_str, i32 0, i32 0), i32 %tmp70)
  ret i32 %tmp70
}

@.fn_IsAlive_str = private constant [8 x i8] c"IsAlive\00"
define i1 @Entity.IsAlive(%Entity* %this) {
entry:
  %tmp71 = getelementptr %Entity, %Entity* %this, i32 0, i32 0
  %tmp72 = load float, float* %tmp71
  %tmp73 = fcmp ogt float %tmp72, 0.0

  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([3 x i8], [3 x i8]* @.fn_IsAlive_str, i32 0, i32 0), i1 %tmp73)
  ret i1 %tmp73
}

