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
  %tmp15 = call i32 @m(i32 7)
  %tmp16 = call i1 @n(i1 1)
  %tmp17 = call i1 @n(i1 0)
  %t_main = alloca i32
  %tmp18 = call i32 @m(i32 8)
  store i32 %tmp18, i32* %t_main
  %u_main = alloca i32
  %tmp19 = call i32 @m(i32 8)
  %tmp20 = add i32 1, %tmp19

  %tmp21 = add i32 %tmp20, 9

  store i32 %tmp21, i32* %u_main
  %defaultInt_main = alloca i32
  %defaultFloat_main = alloca float
  %defaultBool_main = alloca i1
  %tmp22 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 0
  store float 4.0, float* %tmp22
  %default_s32_main = alloca i32
  %default_f32_main = alloca float
  %default_bool_main = alloca i1
  %quad_main = alloca fp128
  store fp128 0xL1C3, fp128* %quad_main
  %i_main = alloca i32
  %tmp23 = sub i32 0, 1
  store i32 %tmp23, i32* %i_main
  %tmp24 = load i32, i32* %i_main
  %tmp25 = call i32 @Abs(i32 %tmp24)
  %tmp26 = load i32, i32* %i_main
  %tmp27 = sub i32 0, %tmp26
  %tmp28 = call i32 @Abs(i32 %tmp27)
  %j_main = alloca float
  %tmp29 = fsub float 0.0, 1.0
  store float %tmp29, float* %j_main
  %fg_main = alloca i1
  store i1 0, i1* %fg_main
  %ffg_main = alloca i1
  store i1 1, i1* %ffg_main
  %a_main = alloca i32
  store i32 5, i32* %a_main
  %b_main = alloca i32
  store i32 10, i32* %b_main
  %c_main = alloca i32
  %tmp30 = load i32, i32* %a_main
  %tmp31 = load i32, i32* %b_main
  %tmp32 = add i32 %tmp30, %tmp31

  store i32 %tmp32, i32* %c_main
  %d_main = alloca i32
  store i32 4, i32* %d_main
  %tmp33 = load i32, i32* %a_main
  %tmp34 = load i32, i32* %c_main
  %tmp35 = add i32 %tmp33, %tmp34

  store i32 %tmp35, i32* %d_main
  %tmp36 = load i32, i32* %d_main
  %tmp37 = add i32 %tmp36, 42

  store i32 %tmp37, i32* %d_main
  %f_main = alloca float
  store float 2.5, float* %f_main
  %tmp38 = load float, float* %f_main
  %tmp39 = fmul float %tmp38, 2.0

  store float %tmp39, float* %f_main
  %g_main = alloca float
  store float 3.0, float* %g_main
  %h_main = alloca float
  %tmp40 = load float, float* %f_main
  %tmp41 = load float, float* %g_main
  %tmp42 = fsub float %tmp40, %tmp41

  store float %tmp42, float* %h_main
  %ok_main = alloca i1
  %tmp43 = load i32, i32* %c_main
  %tmp44 = icmp sgt i32 %tmp43, 5

  store i1 %tmp44, i1* %ok_main
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_main_str, i32 0, i32 0), i32 0)
  ret i32 0
}

@.fn_Branchy_str = private constant [8 x i8] c"Branchy\00"
define i32 @Branchy(i32 %value) {
entry:
  %tmp45 = alloca i32
  store i32 %value, i32* %tmp45
  %tmp46 = load i32, i32* %tmp45
  %tmp47 = icmp eq i32 %tmp46, 0

  br i1 %tmp47, label %if_then_0, label %if_else_0
if_then_0:
  store i32 42, i32* %tmp45
  %f_if_then277 = alloca float
  store float 0.0, float* %f_if_then277
  %tmp48 = fsub float 0.0, 1.0
  store float %tmp48, float* %f_if_then277
  br label %if_end_0
if_else_0:
  %tmp49 = sub i32 0, 42
  store i32 %tmp49, i32* %tmp45
  %f_if_else293 = alloca float
  store float 78.0, float* %f_if_else293
  br label %if_end_0
if_end_0:
  %tmp50 = load i32, i32* %tmp45
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Branchy_str, i32 0, i32 0), i32 %tmp50)
  ret i32 %tmp50
}

@.fn_Abs_str = private constant [4 x i8] c"Abs\00"
define i32 @Abs(i32 %value) {
entry:
  %tmp51 = alloca i32
  store i32 %value, i32* %tmp51
  %tmp52 = load i32, i32* %tmp51
  %tmp53 = icmp slt i32 %tmp52, 0

  br i1 %tmp53, label %if_then_1, label %if_end_1
if_then_1:
  %tmp54 = load i32, i32* %tmp51
  %tmp55 = sub i32 0, %tmp54
  ret i32 %tmp55
  br label %if_end_1
if_end_1:
  %tmp56 = load i32, i32* %tmp51
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Abs_str, i32 0, i32 0), i32 %tmp56)
  ret i32 %tmp56
}

@.fn_Loopy_str = private constant [6 x i8] c"Loopy\00"
define i32 @Loopy(i32 %value) {
entry:
  %tmp57 = alloca i32
  store i32 %value, i32* %tmp57
  %i_Loopy = alloca i32
  store i32 0, i32* %i_Loopy
  br label %for_cond2
for_cond2:
  %tmp58 = load i32, i32* %i_Loopy
  %tmp59 = load i32, i32* %tmp57
  %tmp60 = icmp slt i32 %tmp58, %tmp59

  br i1 %tmp60, label %for_body2, label %for_end2
for_body2:
  %tmp61 = load i32, i32* %tmp57
  %tmp62 = load i32, i32* %i_Loopy
  %tmp63 = add i32 %tmp61, %tmp62

  store i32 %tmp63, i32* %tmp57
  br label %for_iter2
for_iter2:
  %tmp64 = load i32, i32* %i_Loopy
  %tmp65 = sub i32 %tmp64, 3

  store i32 %tmp65, i32* %i_Loopy
  br label %for_cond2
for_end2:
  %tmp66 = load i32, i32* %tmp57
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Loopy_str, i32 0, i32 0), i32 %tmp66)
  ret i32 %tmp66
}

@.fn_n_str = private constant [2 x i8] c"n\00"
define i1 @n(i1 %b) {
entry:
  %tmp67 = alloca i1
  store i1 %b, i1* %tmp67
  %tmp68 = load i1, i1* %tmp67
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([3 x i8], [3 x i8]* @.fn_n_str, i32 0, i32 0), i1 %tmp68)
  ret i1 %tmp68
}

@.fn_m_str = private constant [2 x i8] c"m\00"
define i32 @m(i32 %j) {
entry:
  %tmp69 = alloca i32
  store i32 %j, i32* %tmp69
  %i_m = alloca i32
  store i32 3, i32* %i_m
  %tmp70 = load i32, i32* %tmp69
  %tmp71 = add i32 %tmp70, 4

  store i32 %tmp71, i32* %tmp69
  %health_m = alloca i32
  store i32 7, i32* %health_m
  %tmp72 = load i32, i32* %tmp69
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_m_str, i32 0, i32 0), i32 %tmp72)
  ret i32 %tmp72
}

@.fn_IsAlive_str = private constant [8 x i8] c"IsAlive\00"
define i1 @Entity.IsAlive(%Entity* %this) {
entry:
  %tmp73 = getelementptr %Entity, %Entity* %this, i32 0, i32 0
  %tmp74 = load float, float* %tmp73
  %tmp75 = fcmp ogt float %tmp74, 0.0

  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([3 x i8], [3 x i8]* @.fn_IsAlive_str, i32 0, i32 0), i1 %tmp75)
  ret i1 %tmp75
}

