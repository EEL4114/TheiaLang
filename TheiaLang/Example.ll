; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"
declare i32 @puts(i8*, ...)
@.theia_print_str = private constant[19 x i8] c"Hello from Theia!\0A\00"
declare i32 @printf(i8*, ...)
@.print_ret_fmt = private constant [16 x i8] c"%s returned %d\0A\00"

%Entity = type { %Transform, float, float, i1 }
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
  %voidPtr_main = alloca i8*
  %z_main = alloca float
  %tmp1 = getelementptr inbounds [4 x float], [4 x float]* %Vec4_main, i32 0, i32 2
  %tmp2 = load float, float* %tmp1
  store float %tmp2, float* %z_main
  %entity_main = alloca %Entity
  %tmp3 = alloca %Transform
  %tmp4 = alloca %Vector3
  %tmp5 = getelementptr %Vector3, %Vector3* %tmp4, i32 0, i32 0
  store float 0.0, float* %tmp5
  %tmp6 = getelementptr %Vector3, %Vector3* %tmp4, i32 0, i32 1
  store float 0.0, float* %tmp6
  %tmp7 = getelementptr %Vector3, %Vector3* %tmp4, i32 0, i32 2
  store float 0.0, float* %tmp7
  %tmp8 = load %Vector3, %Vector3* %tmp4
  %tmp9 = getelementptr %Transform, %Transform* %tmp3, i32 0, i32 0
  store %Vector3 %tmp8, %Vector3* %tmp9
  %tmp10 = alloca %Vector3
  %tmp11 = getelementptr %Vector3, %Vector3* %tmp10, i32 0, i32 0
  store float 0.0, float* %tmp11
  %tmp12 = getelementptr %Vector3, %Vector3* %tmp10, i32 0, i32 1
  store float 0.0, float* %tmp12
  %tmp13 = getelementptr %Vector3, %Vector3* %tmp10, i32 0, i32 2
  store float 0.0, float* %tmp13
  %tmp14 = load %Vector3, %Vector3* %tmp10
  %tmp15 = getelementptr %Transform, %Transform* %tmp3, i32 0, i32 1
  store %Vector3 %tmp14, %Vector3* %tmp15
  %tmp16 = alloca %Vector3
  %tmp17 = getelementptr %Vector3, %Vector3* %tmp16, i32 0, i32 0
  store float 0.0, float* %tmp17
  %tmp18 = getelementptr %Vector3, %Vector3* %tmp16, i32 0, i32 1
  store float 0.0, float* %tmp18
  %tmp19 = getelementptr %Vector3, %Vector3* %tmp16, i32 0, i32 2
  store float 0.0, float* %tmp19
  %tmp20 = load %Vector3, %Vector3* %tmp16
  %tmp21 = getelementptr %Transform, %Transform* %tmp3, i32 0, i32 2
  store %Vector3 %tmp20, %Vector3* %tmp21
  %tmp22 = load %Transform, %Transform* %tmp3
  %tmp23 = getelementptr %Entity, %Entity* %entity_main, i32 0, i32 0
  store %Transform %tmp22, %Transform* %tmp23
  %tmp24 = getelementptr %Entity, %Entity* %entity_main, i32 0, i32 1
  store float 70.0, float* %tmp24
  %tmp25 = getelementptr %Entity, %Entity* %entity_main, i32 0, i32 2
  store float 1.5, float* %tmp25
  %tmp26 = getelementptr %Entity, %Entity* %entity_main, i32 0, i32 3
  store i1 0, i1* %tmp26

  %HP_main = alloca float
  %tmp27 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 1
  %tmp28 = load float, float* %tmp27
  store float %tmp28, float* %HP_main
  %healthPtr_main = alloca float*
  store float* %HP_main, float** %healthPtr_main
  %tmp29 = load float*, float** %healthPtr_main
  store float 5.0, float* %tmp29
  %moreHealth_main = alloca float
  %tmp30 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 1
  %tmp31 = load float, float* %tmp30
  %tmp32 = fadd float 1.0, %tmp31

  store float %tmp32, float* %moreHealth_main
  store float* %moreHealth_main, float** %healthPtr_main
  %tmp33 = load float*, float** %healthPtr_main
  %tmp34 = load float, float* %tmp33
  store float %tmp34, float* %HP_main
  %tmp35 = load float, float* %moreHealth_main
  %tmp36 = fadd float %tmp35, 1.0

  store float %tmp36, float* %moreHealth_main
  %tmp37 = call i32 @m(i32 7)
  %tmp38 = call i1 @n(i1 1)
  %tmp39 = call i1 @n(i1 0)
  %t_main = alloca i32
  %tmp40 = call i32 @m(i32 8)
  store i32 %tmp40, i32* %t_main
  %u_main = alloca i32
  %tmp41 = call i32 @m(i32 8)
  %tmp42 = add i32 1, %tmp41

  %tmp43 = add i32 %tmp42, 9

  store i32 %tmp43, i32* %u_main
  %defaultInt_main = alloca i32
  %defaultFloat_main = alloca float
  %defaultBool_main = alloca i1
  %tmp44 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 1
  store float 4.0, float* %tmp44
  %default_s32_main = alloca i32
  %default_f32_main = alloca float
  %default_bool_main = alloca i1
  %quad_main = alloca fp128
  store fp128 0xL1C3, fp128* %quad_main
  %i_main = alloca i32
  store i32 -1, i32* %i_main
  %tmp45 = load i32, i32* %i_main
  %tmp46 = call i32 @Abs(i32 %tmp45)
  %tmp47 = load i32, i32* %i_main
  %tmp48 = sub i32 0, %tmp47
  %tmp49 = call i32 @Abs(i32 %tmp48)
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
  %tmp50 = load i32, i32* %a_main
  %tmp51 = load i32, i32* %b_main
  %tmp52 = add i32 %tmp50, %tmp51

  store i32 %tmp52, i32* %c_main
  %d_main = alloca i32
  store i32 4, i32* %d_main
  %tmp53 = load i32, i32* %a_main
  %tmp54 = load i32, i32* %c_main
  %tmp55 = add i32 %tmp53, %tmp54

  store i32 %tmp55, i32* %d_main
  %tmp56 = load i32, i32* %d_main
  %tmp57 = add i32 %tmp56, 42

  store i32 %tmp57, i32* %d_main
  %f_main = alloca float
  store float 2.5, float* %f_main
  %tmp58 = load float, float* %f_main
  %tmp59 = fmul float %tmp58, 2.0

  store float %tmp59, float* %f_main
  %g_main = alloca float
  store float 3.0, float* %g_main
  %h_main = alloca float
  %tmp60 = load float, float* %f_main
  %tmp61 = load float, float* %g_main
  %tmp62 = fsub float %tmp60, %tmp61

  store float %tmp62, float* %h_main
  %ok_main = alloca i1
  %tmp63 = load i32, i32* %c_main
  %tmp64 = icmp sgt i32 %tmp63, 5

  store i1 %tmp64, i1* %ok_main
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_main_str, i32 0, i32 0), i32 0)
  ret i32 0
}

@.fn_fn_str = private constant [3 x i8] c"fn\00"
define void @fn(i32 %i) {
entry:
  %tmp65 = alloca i32
  store i32 %i, i32* %tmp65
  %j_fn = alloca i32
  %tmp66 = load i32, i32* %tmp65
  %tmp67 = sdiv i32 %tmp66, 7

  store i32 %tmp67, i32* %j_fn
  ret void 
}

@.fn_Branchy_str = private constant [8 x i8] c"Branchy\00"
define i32 @Branchy(i32 %value) {
entry:
  %tmp68 = alloca i32
  store i32 %value, i32* %tmp68
  %tmp69 = load i32, i32* %tmp68
  %tmp70 = icmp eq i32 %tmp69, 0

  br i1 %tmp70, label %if_then_0, label %if_else_0
if_then_0:
  store i32 42, i32* %tmp68
  %f_if_then335 = alloca float
  store float 0.0, float* %f_if_then335
  store float -1.0, float* %f_if_then335
  br label %if_end_0
if_else_0:
  store i32 -42, i32* %tmp68
  %f_if_else351 = alloca float
  store float 78.0, float* %f_if_else351
  br label %if_end_0
if_end_0:
  %tmp71 = load i32, i32* %tmp68
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Branchy_str, i32 0, i32 0), i32 %tmp71)
  ret i32 %tmp71
}

@.fn_Abs_str = private constant [4 x i8] c"Abs\00"
define i32 @Abs(i32 %value) {
entry:
  %tmp72 = alloca i32
  store i32 %value, i32* %tmp72
  %tmp73 = load i32, i32* %tmp72
  %tmp74 = icmp slt i32 %tmp73, 0

  br i1 %tmp74, label %if_then_1, label %if_end_1
if_then_1:
  %tmp75 = load i32, i32* %tmp72
  %tmp76 = sub i32 0, %tmp75
  ret i32 %tmp76
  br label %if_end_1
if_end_1:
  %tmp77 = load i32, i32* %tmp72
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Abs_str, i32 0, i32 0), i32 %tmp77)
  ret i32 %tmp77
}

@.fn_Loopy_str = private constant [6 x i8] c"Loopy\00"
define i32 @Loopy(i32 %value) {
entry:
  %tmp78 = alloca i32
  store i32 %value, i32* %tmp78
  %i_Loopy = alloca i32
  store i32 0, i32* %i_Loopy
  br label %for_cond2
for_cond2:
  %tmp79 = load i32, i32* %i_Loopy
  %tmp80 = load i32, i32* %tmp78
  %tmp81 = icmp slt i32 %tmp79, %tmp80

  br i1 %tmp81, label %for_body2, label %for_end2
for_body2:
  %tmp82 = load i32, i32* %tmp78
  %tmp83 = load i32, i32* %i_Loopy
  %tmp84 = add i32 %tmp82, %tmp83

  store i32 %tmp84, i32* %tmp78
  br label %for_iter2
for_iter2:
  %tmp85 = load i32, i32* %i_Loopy
  %tmp86 = sub i32 %tmp85, 3

  store i32 %tmp86, i32* %i_Loopy
  br label %for_cond2
for_end2:
  %tmp87 = load i32, i32* %tmp78
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Loopy_str, i32 0, i32 0), i32 %tmp87)
  ret i32 %tmp87
}

@.fn_n_str = private constant [2 x i8] c"n\00"
define i1 @n(i1 %b) {
entry:
  %tmp88 = alloca i1
  store i1 %b, i1* %tmp88
  %tmp89 = load i1, i1* %tmp88
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([3 x i8], [3 x i8]* @.fn_n_str, i32 0, i32 0), i1 %tmp89)
  ret i1 %tmp89
}

@.fn_m_str = private constant [2 x i8] c"m\00"
define i32 @m(i32 %j) {
entry:
  %tmp90 = alloca i32
  store i32 %j, i32* %tmp90
  %i_m = alloca i32
  store i32 3, i32* %i_m
  %tmp91 = load i32, i32* %tmp90
  %tmp92 = add i32 %tmp91, 4

  store i32 %tmp92, i32* %tmp90
  %health_m = alloca i32
  store i32 7, i32* %health_m
  %tmp93 = load i32, i32* %tmp90
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_m_str, i32 0, i32 0), i32 %tmp93)
  ret i32 %tmp93
}

@.fn_IsAlive_str = private constant [8 x i8] c"IsAlive\00"
define i1 @Entity.IsAlive(%Entity* %this) {
entry:
  %tmp94 = getelementptr %Entity, %Entity* %this, i32 0, i32 1
  %tmp95 = load float, float* %tmp94
  %tmp96 = fcmp ogt float %tmp95, 0.0

  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([3 x i8], [3 x i8]* @.fn_IsAlive_str, i32 0, i32 0), i1 %tmp96)
  ret i1 %tmp96
}

