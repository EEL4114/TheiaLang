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
  %moreHealth_main = alloca float
  %tmp4 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 0
  %tmp5 = load float, float* %tmp4
  %tmp6 = fadd float 1.0, %tmp5

  store float %tmp6, float* %moreHealth_main
  %tmp7 = load float, float* %moreHealth_main
  %tmp8 = fadd float %tmp7, 1.0

  store float %tmp8, float* %moreHealth_main
  %tmp9 = call i32 @m(i32 7)
  %tmp10 = call i1 @n(i1 1)
  %tmp11 = call i1 @n(i1 0)
  %t_main = alloca i32
  %tmp12 = call i32 @m(i32 8)
  store i32 %tmp12, i32* %t_main
  %u_main = alloca i32
  %tmp13 = call i32 @m(i32 8)
  %tmp14 = add i32 1, %tmp13

  %tmp15 = add i32 %tmp14, 9

  store i32 %tmp15, i32* %u_main
  %defaultInt_main = alloca i32
  %defaultFloat_main = alloca float
  %defaultBool_main = alloca i1
  %tmp16 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 0
  store float 4.0, float* %tmp16
  %default_s32_main = alloca i32
  %default_f32_main = alloca float
  %default_bool_main = alloca i1
  %quad_main = alloca fp128
  store fp128 0xL1C3, fp128* %quad_main
  %i_main = alloca i32
  %tmp17 = sub i32 0, 1
  store i32 %tmp17, i32* %i_main
  %tmp18 = load i32, i32* %i_main
  %tmp19 = call i32 @Abs(i32 %tmp18)
  %tmp20 = load i32, i32* %i_main
  %tmp21 = sub i32 0, %tmp20
  %tmp22 = call i32 @Abs(i32 %tmp21)
  %j_main = alloca float
  %tmp23 = fsub float 0.0, 1.0
  store float %tmp23, float* %j_main
  %fg_main = alloca i1
  store i1 0, i1* %fg_main
  %ffg_main = alloca i1
  store i1 1, i1* %ffg_main
  %a_main = alloca i32
  store i32 5, i32* %a_main
  %b_main = alloca i32
  store i32 10, i32* %b_main
  %c_main = alloca i32
  %tmp24 = load i32, i32* %a_main
  %tmp25 = load i32, i32* %b_main
  %tmp26 = add i32 %tmp24, %tmp25

  store i32 %tmp26, i32* %c_main
  %d_main = alloca i32
  store i32 4, i32* %d_main
  %tmp27 = load i32, i32* %a_main
  %tmp28 = load i32, i32* %c_main
  %tmp29 = add i32 %tmp27, %tmp28

  store i32 %tmp29, i32* %d_main
  %tmp30 = load i32, i32* %d_main
  %tmp31 = add i32 %tmp30, 42

  store i32 %tmp31, i32* %d_main
  %f_main = alloca float
  store float 2.5, float* %f_main
  %tmp32 = load float, float* %f_main
  %tmp33 = fmul float %tmp32, 2.0

  store float %tmp33, float* %f_main
  %g_main = alloca float
  store float 3.0, float* %g_main
  %h_main = alloca float
  %tmp34 = load float, float* %f_main
  %tmp35 = load float, float* %g_main
  %tmp36 = fsub float %tmp34, %tmp35

  store float %tmp36, float* %h_main
  %ok_main = alloca i1
  %tmp37 = load i32, i32* %c_main
  %tmp38 = icmp sgt i32 %tmp37, 5

  store i1 %tmp38, i1* %ok_main
  %tmp39 = load i32, i32* %c_main
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_main_str, i32 0, i32 0), i32 %tmp39)
  ret i32 %tmp39
}

@.fn_Branchy_str = private constant [8 x i8] c"Branchy\00"
define i32 @Branchy(i32 %value) {
entry:
  %tmp40 = alloca i32
  store i32 %value, i32* %tmp40
  %tmp41 = load i32, i32* %tmp40
  %tmp42 = icmp eq i32 %tmp41, 0

  br i1 %tmp42, label %if_then_0, label %if_else_0
if_then_0:
  store i32 42, i32* %tmp40
  %f_if_then242 = alloca float
  store float 0.0, float* %f_if_then242
  %tmp43 = fsub float 0.0, 1.0
  store float %tmp43, float* %f_if_then242
  br label %if_end_0
if_else_0:
  %tmp44 = sub i32 0, 42
  store i32 %tmp44, i32* %tmp40
  %f_if_else258 = alloca float
  store float 78.0, float* %f_if_else258
  br label %if_end_0
if_end_0:
  %tmp45 = load i32, i32* %tmp40
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Branchy_str, i32 0, i32 0), i32 %tmp45)
  ret i32 %tmp45
}

@.fn_Abs_str = private constant [4 x i8] c"Abs\00"
define i32 @Abs(i32 %value) {
entry:
  %tmp46 = alloca i32
  store i32 %value, i32* %tmp46
  %tmp47 = load i32, i32* %tmp46
  %tmp48 = icmp slt i32 %tmp47, 0

  br i1 %tmp48, label %if_then_1, label %if_end_1
if_then_1:
  %tmp49 = load i32, i32* %tmp46
  %tmp50 = sub i32 0, %tmp49
  ret i32 %tmp50
  br label %if_end_1
if_end_1:
  %tmp51 = load i32, i32* %tmp46
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Abs_str, i32 0, i32 0), i32 %tmp51)
  ret i32 %tmp51
}

@.fn_Loopy_str = private constant [6 x i8] c"Loopy\00"
define i32 @Loopy(i32 %value) {
entry:
  %tmp52 = alloca i32
  store i32 %value, i32* %tmp52
  %i_Loopy = alloca i32
  store i32 0, i32* %i_Loopy
  br label %for_cond2
for_cond2:
  %tmp53 = load i32, i32* %i_Loopy
  %tmp54 = load i32, i32* %tmp52
  %tmp55 = icmp slt i32 %tmp53, %tmp54

  br i1 %tmp55, label %for_body2, label %for_end2
for_body2:
  %tmp56 = load i32, i32* %tmp52
  %tmp57 = load i32, i32* %i_Loopy
  %tmp58 = add i32 %tmp56, %tmp57

  store i32 %tmp58, i32* %tmp52
  br label %for_iter2
for_iter2:
  %tmp59 = load i32, i32* %i_Loopy
  %tmp60 = sub i32 %tmp59, 3

  store i32 %tmp60, i32* %i_Loopy
  br label %for_cond2
for_end2:
  %tmp61 = load i32, i32* %tmp52
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Loopy_str, i32 0, i32 0), i32 %tmp61)
  ret i32 %tmp61
}

@.fn_n_str = private constant [2 x i8] c"n\00"
define i1 @n(i1 %b) {
entry:
  %tmp62 = alloca i1
  store i1 %b, i1* %tmp62
  %tmp63 = load i1, i1* %tmp62
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([3 x i8], [3 x i8]* @.fn_n_str, i32 0, i32 0), i1 %tmp63)
  ret i1 %tmp63
}

@.fn_m_str = private constant [2 x i8] c"m\00"
define i32 @m(i32 %j) {
entry:
  %tmp64 = alloca i32
  store i32 %j, i32* %tmp64
  %i_m = alloca i32
  store i32 3, i32* %i_m
  %tmp65 = load i32, i32* %tmp64
  %tmp66 = add i32 %tmp65, 4

  store i32 %tmp66, i32* %tmp64
  %health_m = alloca i32
  store i32 7, i32* %health_m
  %tmp67 = load i32, i32* %tmp64
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_m_str, i32 0, i32 0), i32 %tmp67)
  ret i32 %tmp67
}

@.fn_IsAlive_str = private constant [8 x i8] c"IsAlive\00"
define i1 @Entity.IsAlive(%Entity* %this) {
entry:
  %tmp68 = getelementptr %Entity, %Entity* %this, i32 0, i32 0
  %tmp69 = load float, float* %tmp68
  %tmp70 = fcmp ogt float %tmp69, 0.0

  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([3 x i8], [3 x i8]* @.fn_IsAlive_str, i32 0, i32 0), i1 %tmp70)
  ret i1 %tmp70
}

