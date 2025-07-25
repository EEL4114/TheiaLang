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
  %tmp0 = call i32 @m(i32 7)
  %tmp1 = call i1 @n(i1 1)
  %tmp2 = call i1 @n(i1 0)
  %t_main = alloca i32
  %tmp3 = call i32 @m(i32 8)
  store i32 %tmp3, i32* %t_main
  %u_main = alloca i32
  %tmp4 = call i32 @m(i32 8)
  %tmp5 = add i32 1, %tmp4

  %tmp6 = add i32 %tmp5, 9

  store i32 %tmp6, i32* %u_main
  %entity_main = alloca %Entity
  %tmp7 = getelementptr %Entity, %Entity* %entity_main, i32 0, i32 0
  store float 70.0, float* %tmp7
  %tmp8 = getelementptr %Entity, %Entity* %entity_main, i32 0, i32 1
  store float 1.5, float* %tmp8

  %HP_main = alloca float
  %tmp9 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 0
  %tmp10 = load float, float* %tmp9
  store float %tmp10, float* %HP_main
  %moreHealth_main = alloca float
  %tmp11 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 0
  %tmp12 = load float, float* %tmp11
  %tmp13 = fadd float 1.0, %tmp12

  store float %tmp13, float* %moreHealth_main
  %defaultInt_main = alloca i32
  %defaultFloat_main = alloca float
  %defaultBool_main = alloca i1
  %tmp14 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 0
  store float 4.0, float* %tmp14
  %default_s32_main = alloca i32
  %default_f32_main = alloca float
  %default_bool_main = alloca i1
  %quad_main = alloca fp128
  store fp128 0xL1C3, fp128* %quad_main
  %i_main = alloca i32
  %tmp15 = sub i32 0, 1
  store i32 %tmp15, i32* %i_main
  %tmp16 = load i32, i32* %i_main
  %tmp17 = call i32 @Abs(i32 %tmp16)
  %tmp18 = load i32, i32* %i_main
  %tmp19 = sub i32 0, %tmp18
  %tmp20 = call i32 @Abs(i32 %tmp19)
  %j_main = alloca float
  %tmp21 = fsub float 0.0, 1.0
  store float %tmp21, float* %j_main
  %fg_main = alloca i1
  store i1 0, i1* %fg_main
  %ffg_main = alloca i1
  store i1 1, i1* %ffg_main
  %a_main = alloca i32
  store i32 5, i32* %a_main
  %b_main = alloca i32
  store i32 10, i32* %b_main
  %c_main = alloca i32
  %tmp22 = load i32, i32* %a_main
  %tmp23 = load i32, i32* %b_main
  %tmp24 = add i32 %tmp22, %tmp23

  store i32 %tmp24, i32* %c_main
  %d_main = alloca i32
  store i32 4, i32* %d_main
  %tmp25 = load i32, i32* %a_main
  %tmp26 = load i32, i32* %c_main
  %tmp27 = add i32 %tmp25, %tmp26

  store i32 %tmp27, i32* %d_main
  %tmp28 = load i32, i32* %d_main
  %tmp29 = add i32 %tmp28, 42

  store i32 %tmp29, i32* %d_main
  %f_main = alloca float
  store float 2.5, float* %f_main
  %tmp30 = load float, float* %f_main
  %tmp31 = fmul float %tmp30, 2.0

  store float %tmp31, float* %f_main
  %g_main = alloca float
  store float 3.0, float* %g_main
  %h_main = alloca float
  %tmp32 = load float, float* %f_main
  %tmp33 = load float, float* %g_main
  %tmp34 = fsub float %tmp32, %tmp33

  store float %tmp34, float* %h_main
  %ok_main = alloca i1
  %tmp35 = load i32, i32* %c_main
  %tmp36 = icmp sgt i32 %tmp35, 5

  store i1 %tmp36, i1* %ok_main
  %tmp37 = load i32, i32* %c_main
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_main_str, i32 0, i32 0), i32 %tmp37)
  ret i32 %tmp37
}

@.fn_Branchy_str = private constant [8 x i8] c"Branchy\00"
define i32 @Branchy(i32 %value) {
entry:
  %tmp38 = alloca i32
  store i32 %value, i32* %tmp38
  %tmp39 = load i32, i32* %tmp38
  %tmp40 = icmp eq i32 %tmp39, 0

  br i1 %tmp40, label %if_then_0, label %if_else_0
if_then_0:
  store i32 42, i32* %tmp38
  %f_if_then231 = alloca float
  store float 0.0, float* %f_if_then231
  %tmp41 = fsub float 0.0, 1.0
  store float %tmp41, float* %f_if_then231
  br label %if_end_0
if_else_0:
  %tmp42 = sub i32 0, 42
  store i32 %tmp42, i32* %tmp38
  %f_if_else247 = alloca float
  store float 78.0, float* %f_if_else247
  br label %if_end_0
if_end_0:
  %tmp43 = load i32, i32* %tmp38
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Branchy_str, i32 0, i32 0), i32 %tmp43)
  ret i32 %tmp43
}

@.fn_Abs_str = private constant [4 x i8] c"Abs\00"
define i32 @Abs(i32 %value) {
entry:
  %tmp44 = alloca i32
  store i32 %value, i32* %tmp44
  %tmp45 = load i32, i32* %tmp44
  %tmp46 = icmp slt i32 %tmp45, 0

  br i1 %tmp46, label %if_then_1, label %if_end_1
if_then_1:
  %tmp47 = load i32, i32* %tmp44
  %tmp48 = sub i32 0, %tmp47
  ret i32 %tmp48
  br label %if_end_1
if_end_1:
  %tmp49 = load i32, i32* %tmp44
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Abs_str, i32 0, i32 0), i32 %tmp49)
  ret i32 %tmp49
}

@.fn_Loopy_str = private constant [6 x i8] c"Loopy\00"
define i32 @Loopy(i32 %value) {
entry:
  %tmp50 = alloca i32
  store i32 %value, i32* %tmp50
  %i_Loopy = alloca i32
  store i32 0, i32* %i_Loopy
  br label %for_cond2
for_cond2:
  %tmp51 = load i32, i32* %i_Loopy
  %tmp52 = load i32, i32* %tmp50
  %tmp53 = icmp slt i32 %tmp51, %tmp52

  br i1 %tmp53, label %for_body2, label %for_end2
for_body2:
  %tmp54 = load i32, i32* %tmp50
  %tmp55 = load i32, i32* %i_Loopy
  %tmp56 = add i32 %tmp54, %tmp55

  store i32 %tmp56, i32* %tmp50
  br label %for_iter2
for_iter2:
  %tmp57 = load i32, i32* %i_Loopy
  %tmp58 = sub i32 %tmp57, 3

  store i32 %tmp58, i32* %i_Loopy
  br label %for_cond2
for_end2:
  %tmp59 = load i32, i32* %tmp50
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Loopy_str, i32 0, i32 0), i32 %tmp59)
  ret i32 %tmp59
}

@.fn_n_str = private constant [2 x i8] c"n\00"
define i1 @n(i1 %b) {
entry:
  %tmp60 = alloca i1
  store i1 %b, i1* %tmp60
  %tmp61 = load i1, i1* %tmp60
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([3 x i8], [3 x i8]* @.fn_n_str, i32 0, i32 0), i1 %tmp61)
  ret i1 %tmp61
}

@.fn_m_str = private constant [2 x i8] c"m\00"
define i32 @m(i32 %j) {
entry:
  %tmp62 = alloca i32
  store i32 %j, i32* %tmp62
  %i_m = alloca i32
  store i32 3, i32* %i_m
  %tmp63 = load i32, i32* %tmp62
  %tmp64 = add i32 %tmp63, 4

  store i32 %tmp64, i32* %tmp62
  %health_m = alloca i32
  store i32 7, i32* %health_m
  %tmp65 = load i32, i32* %tmp62
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_m_str, i32 0, i32 0), i32 %tmp65)
  ret i32 %tmp65
}

@.fn_IsAlive_str = private constant [8 x i8] c"IsAlive\00"
define i1 @Entity.IsAlive(%Entity* %this) {
entry:
  %tmp66 = getelementptr %Entity, %Entity* %this, i32 0, i32 0
  %tmp67 = load float, float* %tmp66
  %tmp68 = fcmp ogt float %tmp67, 0.0

  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([3 x i8], [3 x i8]* @.fn_IsAlive_str, i32 0, i32 0), i1 %tmp68)
  ret i1 %tmp68
}

