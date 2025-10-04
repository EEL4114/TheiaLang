; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"

declare noalias ptr @realloc(i64)
declare noalias ptr @malloc(i64) nounwind willreturn
declare void @free(ptr) nounwind

%theia.header = type { i64, i64 }           ; { sizeBytes, magic }
@theia.magic  = internal constant i64 4114, align 8

define internal noalias ptr @__th_allocB(i64 %n) nounwind allocsize(0) {
entry:
  ; total = (n == 0 ? 16 : n + 16)
  %is0   = icmp eq i64 %n, 0
  %np16  = add i64 %n, 16
  %total = select i1 %is0, i64 16, i64 %np16

  %raw   = call noalias ptr @malloc(i64 %total)

  ; write header
  %h_size  = getelementptr %theia.header, ptr %raw, i32 0, i32 0
  store i64 %n, ptr %h_size
  %h_magic = getelementptr %theia.header, ptr %raw, i32 0, i32 1
  %mval    = load i64, ptr @theia.magic
  store i64 %mval, ptr %h_magic

  ; return user pointer = raw + 16
  %user = getelementptr i8, ptr %raw, i64 16
  ret ptr %user
}

define internal noalias ptr @__th_reallocB(ptr %user, i64 %newB) nounwind {
entry:
  ; user ptr == null → behaves like alloc
  %isNull = icmp eq ptr %user, null
  br i1 %isNull, label %alloc, label %have

alloc:
  %retA = call noalias ptr @__th_allocB(i64 %newB)
  ret ptr %retA

have:
  ; newBytes == 0 → free and return null (so arrays can have data = null, cap = 0)
  %isZero = icmp eq i64 %newB, 0
  br i1 %isZero, label %freeNull, label %grow

freeNull:
  call void @__th_free(ptr %user)
  ret ptr null

grow:
  %raw    = getelementptr i8, ptr %user, i64 -16
  %total  = add i64 %newB, 16
  %newRaw = call ptr @realloc(ptr %raw, i64 %total)
  ; update header.size
  %h_sz = getelementptr %theia.header, ptr %newRaw, i32 0, i32 0
  store i64 %newB, ptr %h_sz
  ; return user pointer
  %user2 = getelementptr i8, ptr %newRaw, i64 16
  ret ptr %user2
}

define internal void @__th_free(ptr %user) nounwind {
entry:
  ; grab beginning of the allocation header
  %raw     = getelementptr i8, ptr %user, i64 -16
  %h_magic = getelementptr %theia.header, ptr %raw, i32 0, i32 1
  store i64 0, ptr %h_magic
  call void @free(ptr %raw)
  ret void
}

define internal i64 @theia.__th_alloc_size(ptr %user) nounwind {
entry:
  ; grab beginning of the allocation header
  %raw  = getelementptr i8, ptr %user, i64 -16
  %h_sz = getelementptr %theia.header, ptr %raw, i32 0, i32 0
  %n    = load i64, ptr %h_sz
  ret i64 %n
}

; =============================================================================

; ModuleID = 'theia_module'
@.theia_print_str = private constant[19 x i8] c"Hello from Theia!\0A\00"
declare i32 @printf(ptr, ...)
@.print_ret_fmt = private constant [16 x i8] c"%s returned %d\0A\00"

%Vector2 = type { float, float }
%Entity = type { %Transform, float, float, i1 }
%Transform = type { %Vector3, %Vector3, %Vector3 }
%Vector3 = type { float, float, float }
%Dynamic_Array_s8 = type { i64, ptr, i64 }

@.fn_main_str = private constant [5 x i8] c"main\00"
define i32 @main() {
entry:
  %Vec4_main = alloca [4 x float]
  %tmp0 = getelementptr inbounds [4 x float], ptr %Vec4_main, i32 0, i32 1
  store float 3.0, ptr %tmp0
  %ff_main = alloca %Dynamic_Array_s8
  %arrayOfArrays_main = alloca [4 x [6 x i8]]
  %arrX_main = alloca [4 x [6 x ptr]]
  %arrY_main = alloca [4 x ptr]
  %ptrToArrayOfArrays_main = alloca ptr
  %ptrToFloatArray_main = alloca ptr
  %arrOfFloatPtrs_main = alloca [4 x ptr]
  %v_main = alloca ptr
  %tmp1 = call ptr @AllocB(i64 4)
  store ptr %tmp1, ptr %v_main
  %tmp2 = load ptr, ptr %v_main
  %tmp3 = call ptr @ReallocB(ptr %tmp2, i64 8)
  store ptr %tmp3, ptr %v_main
  %voidPtr_main = alloca ptr
  %dynamicArray_main = alloca %Dynamic_Array_s8
  %dynamicArray2_main = alloca %Dynamic_Array_s8
  %z_main = alloca float
  %tmp4 = getelementptr inbounds [4 x float], ptr %Vec4_main, i32 0, i32 2
  %tmp5 = load float, ptr %tmp4
  store float %tmp5, ptr %z_main
  %entity_main = alloca %Entity
  %tmp6 = alloca %Transform
  %tmp7 = alloca %Vector3
  %tmp8 = getelementptr %Vector3, ptr %tmp7, i32 0, i32 0
  store float 1.0, ptr %tmp8
  %tmp9 = getelementptr %Vector3, ptr %tmp7, i32 0, i32 1
  store float 1.0, ptr %tmp9
  %tmp10 = getelementptr %Vector3, ptr %tmp7, i32 0, i32 2
  store float 1.0, ptr %tmp10
  %tmp11 = load %Vector3, ptr %tmp7
  %tmp12 = getelementptr %Transform, ptr %tmp6, i32 0, i32 0
  store %Vector3 %tmp11, ptr %tmp12
  %tmp13 = alloca %Vector3
  %tmp14 = getelementptr %Vector3, ptr %tmp13, i32 0, i32 0
  store float 1.0, ptr %tmp14
  %tmp15 = getelementptr %Vector3, ptr %tmp13, i32 0, i32 1
  store float 1.0, ptr %tmp15
  %tmp16 = getelementptr %Vector3, ptr %tmp13, i32 0, i32 2
  store float 1.0, ptr %tmp16
  %tmp17 = load %Vector3, ptr %tmp13
  %tmp18 = getelementptr %Transform, ptr %tmp6, i32 0, i32 1
  store %Vector3 %tmp17, ptr %tmp18
  %tmp19 = alloca %Vector3
  %tmp20 = getelementptr %Vector3, ptr %tmp19, i32 0, i32 0
  store float 1.0, ptr %tmp20
  %tmp21 = getelementptr %Vector3, ptr %tmp19, i32 0, i32 1
  store float 1.0, ptr %tmp21
  %tmp22 = getelementptr %Vector3, ptr %tmp19, i32 0, i32 2
  store float 1.0, ptr %tmp22
  %tmp23 = load %Vector3, ptr %tmp19
  %tmp24 = getelementptr %Transform, ptr %tmp6, i32 0, i32 2
  store %Vector3 %tmp23, ptr %tmp24
  %tmp25 = load %Transform, ptr %tmp6
  %tmp26 = getelementptr %Entity, ptr %entity_main, i32 0, i32 0
  store %Transform %tmp25, ptr %tmp26
  %tmp27 = getelementptr %Entity, ptr %entity_main, i32 0, i32 1
  store float 70.0, ptr %tmp27
  %tmp28 = getelementptr %Entity, ptr %entity_main, i32 0, i32 2
  store float 1.5, ptr %tmp28
  %tmp29 = getelementptr %Entity, ptr %entity_main, i32 0, i32 3
  store i1 0, ptr %tmp29

  %ii_main = alloca i32
  store i32 45, ptr %ii_main
  %jj_main = alloca i32
  store i32 4, ptr %jj_main
  %HP_main = alloca float
  %tmp30 = getelementptr inbounds %Entity, ptr %entity_main, i32 0, i32 1
  %tmp31 = load float, ptr %tmp30
  store float %tmp31, ptr %HP_main
  %healthPtr_main = alloca ptr
  store ptr %HP_main, ptr %healthPtr_main
  %tmp32 = load ptr, ptr %healthPtr_main
  store float 5.0, ptr %tmp32
  %moreHealth_main = alloca float
  %tmp33 = getelementptr inbounds %Entity, ptr %entity_main, i32 0, i32 1
  %tmp34 = load float, ptr %tmp33
  %tmp35 = fadd float 1.0, %tmp34

  store float %tmp35, ptr %moreHealth_main
  store ptr %moreHealth_main, ptr %healthPtr_main
  %tmp36 = load ptr, ptr %healthPtr_main
  store ptr %tmp36, ptr %voidPtr_main
  %tmp37 = load ptr, ptr %healthPtr_main
  %tmp38 = load float, ptr %tmp37
  store float %tmp38, ptr %HP_main
  %tmp39 = load float, ptr %moreHealth_main
  %tmp40 = fadd float %tmp39, 1.0

  store float %tmp40, ptr %moreHealth_main
  %tmp41 = call i32 @m(i32 7)
  %tmp42 = call i1 @n(i1 1)
  %tmp43 = call i1 @n(i1 0)
  %t_main = alloca i32
  %tmp44 = call i32 @m(i32 8)
  store i32 %tmp44, ptr %t_main
  %u_main = alloca i32
  %tmp45 = call i32 @m(i32 8)
  %tmp46 = add i32 1, %tmp45

  %tmp47 = add i32 %tmp46, 9

  store i32 %tmp47, ptr %u_main
  %defaultInt_main = alloca i32
  %defaultFloat_main = alloca float
  %defaultBool_main = alloca i1
  %tmp48 = getelementptr inbounds %Entity, ptr %entity_main, i32 0, i32 1
  store float 4.0, ptr %tmp48
  %default_s32_main = alloca i32
  %default_f32_main = alloca float
  %default_bool_main = alloca i1
  %quad_main = alloca fp128
  store fp128 0xL1C3, ptr %quad_main
  %i_main = alloca i32
  store i32 -1, ptr %i_main
  %tmp49 = load i32, ptr %i_main
  %tmp50 = call i32 @Abs(i32 %tmp49)
  %tmp51 = load i32, ptr %i_main
  %tmp52 = sub i32 0, %tmp51
  %tmp53 = call i32 @Abs(i32 %tmp52)
  %j_main = alloca float
  store float -1.0, ptr %j_main
  %fg_main = alloca i1
  store i1 0, ptr %fg_main
  %ffg_main = alloca i1
  store i1 1, ptr %ffg_main
  %a_main = alloca i32
  store i32 5, ptr %a_main
  %b_main = alloca i32
  store i32 10, ptr %b_main
  %c_main = alloca i32
  %tmp54 = load i32, ptr %a_main
  %tmp55 = load i32, ptr %b_main
  %tmp56 = add i32 %tmp54, %tmp55

  store i32 %tmp56, ptr %c_main
  %d_main = alloca i32
  store i32 4, ptr %d_main
  %tmp57 = load i32, ptr %a_main
  %tmp58 = load i32, ptr %c_main
  %tmp59 = add i32 %tmp57, %tmp58

  store i32 %tmp59, ptr %d_main
  %tmp60 = load i32, ptr %d_main
  %tmp61 = add i32 %tmp60, 42

  store i32 %tmp61, ptr %d_main
  %f_main = alloca float
  store float 2.5, ptr %f_main
  %tmp62 = load float, ptr %f_main
  %tmp63 = fmul float %tmp62, 2.0

  store float %tmp63, ptr %f_main
  %g_main = alloca float
  store float 3.0, ptr %g_main
  %h_main = alloca float
  %tmp64 = load float, ptr %f_main
  %tmp65 = load float, ptr %g_main
  %tmp66 = fsub float %tmp64, %tmp65

  store float %tmp66, ptr %h_main
  %ok_main = alloca i1
  %tmp67 = load i32, ptr %c_main
  %tmp68 = icmp sgt i32 %tmp67, 5

  store i1 %tmp68, ptr %ok_main
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_main_str, i32 0, i32 0), i32 0)
  ret i32 0
}

@.fn_fn_str = private constant [3 x i8] c"fn\00"
define void @fn(i32 %i) {
entry:
  %tmp69 = alloca i32
  store i32 %i, ptr %tmp69
  %j_fn = alloca i32
  %tmp70 = load i32, ptr %tmp69
  %tmp71 = sdiv i32 %tmp70, 7

  store i32 %tmp71, ptr %j_fn
  ret void 
}

@.fn_Branchy_str = private constant [8 x i8] c"Branchy\00"
define i32 @Branchy(i32 %value) {
entry:
  %tmp72 = alloca i32
  store i32 %value, ptr %tmp72
  %tmp73 = load i32, ptr %tmp72
  %tmp74 = icmp eq i32 %tmp73, 0

  br i1 %tmp74, label %if_then_0, label %if_else_0
if_then_0:
  store i32 42, ptr %tmp72
  %f_if_then440 = alloca float
  store float 0.0, ptr %f_if_then440
  store float -1.0, ptr %f_if_then440
  br label %if_end_0
if_else_0:
  store i32 -42, ptr %tmp72
  %f_if_else456 = alloca float
  store float 78.0, ptr %f_if_else456
  br label %if_end_0
if_end_0:
  %tmp75 = load i32, ptr %tmp72
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_Branchy_str, i32 0, i32 0), i32 %tmp75)
  ret i32 %tmp75
}

@.fn_Abs_str = private constant [4 x i8] c"Abs\00"
define i32 @Abs(i32 %value) {
entry:
  %tmp76 = alloca i32
  store i32 %value, ptr %tmp76
  %tmp77 = load i32, ptr %tmp76
  %tmp78 = icmp slt i32 %tmp77, 0

  br i1 %tmp78, label %if_then_1, label %if_end_1
if_then_1:
  %tmp79 = load i32, ptr %tmp76
  %tmp80 = sub i32 0, %tmp79
  ret i32 %tmp80
  br label %if_end_1
if_end_1:
  %tmp81 = load i32, ptr %tmp76
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_Abs_str, i32 0, i32 0), i32 %tmp81)
  ret i32 %tmp81
}

@.fn_Loopy_str = private constant [6 x i8] c"Loopy\00"
define i32 @Loopy(i32 %value) {
entry:
  %tmp82 = alloca i32
  store i32 %value, ptr %tmp82
  %i_Loopy = alloca i32
  store i32 0, ptr %i_Loopy
  br label %for_cond2
for_cond2:
  %tmp83 = load i32, ptr %i_Loopy
  %tmp84 = load i32, ptr %tmp82
  %tmp85 = icmp slt i32 %tmp83, %tmp84

  br i1 %tmp85, label %for_body2, label %for_end2
for_body2:
  %tmp86 = load i32, ptr %tmp82
  %tmp87 = load i32, ptr %i_Loopy
  %tmp88 = add i32 %tmp86, %tmp87

  store i32 %tmp88, ptr %tmp82
  br label %for_iter2
for_iter2:
  %tmp89 = load i32, ptr %i_Loopy
  %tmp90 = sub i32 %tmp89, 3

  store i32 %tmp90, ptr %i_Loopy
  br label %for_cond2
for_end2:
  %tmp91 = load i32, ptr %tmp82
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_Loopy_str, i32 0, i32 0), i32 %tmp91)
  ret i32 %tmp91
}

@.fn_n_str = private constant [2 x i8] c"n\00"
define i1 @n(i1 %b) {
entry:
  %tmp92 = alloca i1
  store i1 %b, ptr %tmp92
  %tmp93 = load i1, ptr %tmp92
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([3 x i8], ptr @.fn_n_str, i32 0, i32 0), i1 %tmp93)
  ret i1 %tmp93
}

@.fn_m_str = private constant [2 x i8] c"m\00"
define i32 @m(i32 %j) {
entry:
  %tmp94 = alloca i32
  store i32 %j, ptr %tmp94
  %i_m = alloca i32
  store i32 3, ptr %i_m
  %tmp95 = load i32, ptr %tmp94
  %tmp96 = add i32 %tmp95, 4

  store i32 %tmp96, ptr %tmp94
  %health_m = alloca i32
  store i32 7, ptr %health_m
  %tmp97 = load i32, ptr %tmp94
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_m_str, i32 0, i32 0), i32 %tmp97)
  ret i32 %tmp97
}

@.fn_IsAlive_str = private constant [8 x i8] c"IsAlive\00"
define i1 @Entity.IsAlive(ptr %this) {
entry:
  %tmp98 = getelementptr %Entity, ptr %this, i32 0, i32 1
  %tmp99 = load float, ptr %tmp98
  %tmp100 = fcmp ogt float %tmp99, 0.0

  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([3 x i8], ptr @.fn_IsAlive_str, i32 0, i32 0), i1 %tmp100)
  ret i1 %tmp100
}

@.fn_AllocB_str = private constant [7 x i8] c"AllocB\00"
define ptr @AllocB(i64 %size) {
entry:
  %tmp101 = alloca i64
  store i64 %size, ptr %tmp101
  %tmp102 = load i64, ptr %tmp101
  %tmp103 = call ptr @__th_allocB(i64 %tmp102)
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_AllocB_str, i32 0, i32 0), ptr %tmp103)
  ret ptr %tmp103
}

@.fn_ReallocB_str = private constant [9 x i8] c"ReallocB\00"
define ptr @ReallocB(ptr %ptr, i64 %newSize) {
entry:
  %tmp104 = alloca ptr
  store ptr %ptr, ptr %tmp104
  %tmp105 = alloca i64
  store i64 %newSize, ptr %tmp105
  %tmp106 = load ptr, ptr %tmp104
  %tmp107 = load i64, ptr %tmp105
  %tmp108 = call ptr @__th_reallocB(ptr %tmp106, i64 %tmp107)
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_ReallocB_str, i32 0, i32 0), ptr %tmp108)
  ret ptr %tmp108
}

@.fn_Free_str = private constant [5 x i8] c"Free\00"
define void @Free(ptr %ptr) {
entry:
  %tmp109 = alloca ptr
  store ptr %ptr, ptr %tmp109
  %tmp110 = load ptr, ptr %tmp109
  call void @__th_free(ptr %tmp110)
  ret void 
}

