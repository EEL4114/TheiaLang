; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"
declare i32 @puts(i8*, ...)
@.theia_print_str = private constant[19 x i8] c"Hello from Theia!\0A\00"
declare i32 @printf(i8*, ...)
@.print_ret_fmt = private constant [16 x i8] c"%s returned %d\0A\00"

%Entity = type { float, float, i1 }
%Transform = type { %Vector3, %Vector3, %Vector3 }
%Vector3 = type { float, float, float }

@.fn_main_str = private constant [5 x i8] c"main\00"
define i32 @main() {
entry:
  %Vec4_main = alloca [4 x float]
  %tmp0 = getelementptr inbounds [4 x float], [4 x float]* %Vec4_main, i32 0, i32 1
  store float 3.0, float* %tmp0
  %ptrToFloatArray_main = alloca [4 x float]*
  %arrOfFloatPtrs_main = alloca [4 x float*]
  %z_main = alloca float
  %tmp1 = getelementptr inbounds [4 x float], [4 x float]* %Vec4_main, i32 0, i32 2
  %tmp2 = load float, float* %tmp1
  store float %tmp2, float* %z_main
  %entity_main = alloca %Entity
  %tmp3 = getelementptr %Entity, %Entity* %entity_main, i32 0, i32 0
  store float 70.0, float* %tmp3
  %tmp4 = getelementptr %Entity, %Entity* %entity_main, i32 0, i32 1
  store float 1.5, float* %tmp4
  %tmp5 = getelementptr %Entity, %Entity* %entity_main, i32 0, i32 2
  store i1 0, i1* %tmp5

  %HP_main = alloca float
  %tmp6 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 0
  %tmp7 = load float, float* %tmp6
  store float %tmp7, float* %HP_main
  %healthPtr_main = alloca float*
  store float* %HP_main, float** %healthPtr_main
  %tmp8 = load float*, float** %healthPtr_main
  store float 5.0, float* %tmp8
  %moreHealth_main = alloca float
  %tmp9 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 0
  %tmp10 = load float, float* %tmp9
  %tmp11 = fadd float 1.0, %tmp10

  store float %tmp11, float* %moreHealth_main
  store float* %moreHealth_main, float** %healthPtr_main
  %tmp12 = load float*, float** %healthPtr_main
  %tmp13 = load float, float* %tmp12
  store float %tmp13, float* %HP_main
  %tmp14 = load float, float* %moreHealth_main
  %tmp15 = fadd float %tmp14, 1.0

  store float %tmp15, float* %moreHealth_main
  %tmp16 = call i32 @m(i32 7)
  %tmp17 = call i1 @n(i1 1)
  %tmp18 = call i1 @n(i1 0)
  %t_main = alloca i32
  %tmp19 = call i32 @m(i32 8)
  store i32 %tmp19, i32* %t_main
  %u_main = alloca i32
  %tmp20 = call i32 @m(i32 8)
  %tmp21 = add i32 1, %tmp20

  %tmp22 = add i32 %tmp21, 9

  store i32 %tmp22, i32* %u_main
  %defaultInt_main = alloca i32
  %defaultFloat_main = alloca float
  %defaultBool_main = alloca i1
  %tmp23 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 0
  store float 4.0, float* %tmp23
  %default_s32_main = alloca i32
  %default_f32_main = alloca float
  %default_bool_main = alloca i1
  %quad_main = alloca fp128
  store fp128 0xL1C3, fp128* %quad_main
  %i_main = alloca i32
  store i32 -1, i32* %i_main
  %tmp24 = load i32, i32* %i_main
  %tmp25 = call i32 @Abs(i32 %tmp24)
  %tmp26 = load i32, i32* %i_main
  %tmp27 = sub i32 0, %tmp26
  %tmp28 = call i32 @Abs(i32 %tmp27)
  %j_main = alloca float
  store float -1.0, float* %j_main
  %fg_main = alloca i1
  store i1 0, i1* %fg_main
  %ffg_main = alloca i1
  store i1 1, i1* %ffg_main
  %a_main = alloca i32
  store i32 5, i32* %a_main
  %b_main = alloca i32
  store i32 10, i32* %b_main
  %c_main = alloca i32
  %tmp29 = load i32, i32* %a_main
  %tmp30 = load i32, i32* %b_main
  %tmp31 = add i32 %tmp29, %tmp30

  store i32 %tmp31, i32* %c_main
  %d_main = alloca i32
  store i32 4, i32* %d_main
  %tmp32 = load i32, i32* %a_main
  %tmp33 = load i32, i32* %c_main
  %tmp34 = add i32 %tmp32, %tmp33

  store i32 %tmp34, i32* %d_main
  %tmp35 = load i32, i32* %d_main
  %tmp36 = add i32 %tmp35, 42

  store i32 %tmp36, i32* %d_main
  %f_main = alloca float
  store float 2.5, float* %f_main
  %tmp37 = load float, float* %f_main
  %tmp38 = fmul float %tmp37, 2.0

  store float %tmp38, float* %f_main
  %g_main = alloca float
  store float 3.0, float* %g_main
  %h_main = alloca float
  %tmp39 = load float, float* %f_main
  %tmp40 = load float, float* %g_main
  %tmp41 = fsub float %tmp39, %tmp40

  store float %tmp41, float* %h_main
  %ok_main = alloca i1
  %tmp42 = load i32, i32* %c_main
  %tmp43 = icmp sgt i32 %tmp42, 5

  store i1 %tmp43, i1* %ok_main
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_main_str, i32 0, i32 0), i32 0)
  ret i32 0
}

@.fn_Branchy_str = private constant [8 x i8] c"Branchy\00"
define i32 @Branchy(i32 %value) {
entry:
  %tmp44 = alloca i32
  store i32 %value, i32* %tmp44
  %tmp45 = load i32, i32* %tmp44
  %tmp46 = icmp eq i32 %tmp45, 0

  br i1 %tmp46, label %if_then_0, label %if_else_0
if_then_0:
  store i32 42, i32* %tmp44
  %f_if_then280 = alloca float
  store float 0.0, float* %f_if_then280
  store float -1.0, float* %f_if_then280
  br label %if_end_0
if_else_0:
  store i32 -42, i32* %tmp44
  %f_if_else296 = alloca float
  store float 78.0, float* %f_if_else296
  br label %if_end_0
if_end_0:
  %tmp47 = load i32, i32* %tmp44
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Branchy_str, i32 0, i32 0), i32 %tmp47)
  ret i32 %tmp47
}

@.fn_Abs_str = private constant [4 x i8] c"Abs\00"
define i32 @Abs(i32 %value) {
entry:
  %tmp48 = alloca i32
  store i32 %value, i32* %tmp48
  %tmp49 = load i32, i32* %tmp48
  %tmp50 = icmp slt i32 %tmp49, 0

  br i1 %tmp50, label %if_then_1, label %if_end_1
if_then_1:
  %tmp51 = load i32, i32* %tmp48
  %tmp52 = sub i32 0, %tmp51
  ret i32 %tmp52
  br label %if_end_1
if_end_1:
  %tmp53 = load i32, i32* %tmp48
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Abs_str, i32 0, i32 0), i32 %tmp53)
  ret i32 %tmp53
}

@.fn_Loopy_str = private constant [6 x i8] c"Loopy\00"
define i32 @Loopy(i32 %value) {
entry:
  %tmp54 = alloca i32
  store i32 %value, i32* %tmp54
  %i_Loopy = alloca i32
  store i32 0, i32* %i_Loopy
  br label %for_cond2
for_cond2:
  %tmp55 = load i32, i32* %i_Loopy
  %tmp56 = load i32, i32* %tmp54
  %tmp57 = icmp slt i32 %tmp55, %tmp56

  br i1 %tmp57, label %for_body2, label %for_end2
for_body2:
  %tmp58 = load i32, i32* %tmp54
  %tmp59 = load i32, i32* %i_Loopy
  %tmp60 = add i32 %tmp58, %tmp59

  store i32 %tmp60, i32* %tmp54
  br label %for_iter2
for_iter2:
  %tmp61 = load i32, i32* %i_Loopy
  %tmp62 = sub i32 %tmp61, 3

  store i32 %tmp62, i32* %i_Loopy
  br label %for_cond2
for_end2:
  %tmp63 = load i32, i32* %tmp54
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Loopy_str, i32 0, i32 0), i32 %tmp63)
  ret i32 %tmp63
}

@.fn_n_str = private constant [2 x i8] c"n\00"
define i1 @n(i1 %b) {
entry:
  %tmp64 = alloca i1
  store i1 %b, i1* %tmp64
  %tmp65 = load i1, i1* %tmp64
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([3 x i8], [3 x i8]* @.fn_n_str, i32 0, i32 0), i1 %tmp65)
  ret i1 %tmp65
}

@.fn_m_str = private constant [2 x i8] c"m\00"
define i32 @m(i32 %j) {
entry:
  %tmp66 = alloca i32
  store i32 %j, i32* %tmp66
  %i_m = alloca i32
  store i32 3, i32* %i_m
  %tmp67 = load i32, i32* %tmp66
  %tmp68 = add i32 %tmp67, 4

  store i32 %tmp68, i32* %tmp66
  %health_m = alloca i32
  store i32 7, i32* %health_m
  %tmp69 = load i32, i32* %tmp66
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_m_str, i32 0, i32 0), i32 %tmp69)
  ret i32 %tmp69
}

@.fn_IsAlive_str = private constant [8 x i8] c"IsAlive\00"
define i1 @Entity.IsAlive(%Entity* %this) {
entry:
  %tmp70 = getelementptr %Entity, %Entity* %this, i32 0, i32 0
  %tmp71 = load float, float* %tmp70
  %tmp72 = fcmp ogt float %tmp71, 0.0

  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([3 x i8], [3 x i8]* @.fn_IsAlive_str, i32 0, i32 0), i1 %tmp72)
  ret i1 %tmp72
}

