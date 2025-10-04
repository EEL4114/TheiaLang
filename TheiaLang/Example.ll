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
  %voidPtr_main = alloca ptr
  %dynamicArray_main = alloca %Dynamic_Array_s8
  %dynamicArray2_main = alloca %Dynamic_Array_s8
  %z_main = alloca float
  %tmp1 = getelementptr inbounds [4 x float], ptr %Vec4_main, i32 0, i32 2
  %tmp2 = load float, ptr %tmp1
  store float %tmp2, ptr %z_main
  %entity_main = alloca %Entity
  %tmp3 = alloca %Transform
  %tmp4 = alloca %Vector3
  %tmp5 = getelementptr %Vector3, ptr %tmp4, i32 0, i32 0
  store float 1.0, ptr %tmp5
  %tmp6 = getelementptr %Vector3, ptr %tmp4, i32 0, i32 1
  store float 1.0, ptr %tmp6
  %tmp7 = getelementptr %Vector3, ptr %tmp4, i32 0, i32 2
  store float 1.0, ptr %tmp7
  %tmp8 = load %Vector3, ptr %tmp4
  %tmp9 = getelementptr %Transform, ptr %tmp3, i32 0, i32 0
  store %Vector3 %tmp8, ptr %tmp9
  %tmp10 = alloca %Vector3
  %tmp11 = getelementptr %Vector3, ptr %tmp10, i32 0, i32 0
  store float 1.0, ptr %tmp11
  %tmp12 = getelementptr %Vector3, ptr %tmp10, i32 0, i32 1
  store float 1.0, ptr %tmp12
  %tmp13 = getelementptr %Vector3, ptr %tmp10, i32 0, i32 2
  store float 1.0, ptr %tmp13
  %tmp14 = load %Vector3, ptr %tmp10
  %tmp15 = getelementptr %Transform, ptr %tmp3, i32 0, i32 1
  store %Vector3 %tmp14, ptr %tmp15
  %tmp16 = alloca %Vector3
  %tmp17 = getelementptr %Vector3, ptr %tmp16, i32 0, i32 0
  store float 1.0, ptr %tmp17
  %tmp18 = getelementptr %Vector3, ptr %tmp16, i32 0, i32 1
  store float 1.0, ptr %tmp18
  %tmp19 = getelementptr %Vector3, ptr %tmp16, i32 0, i32 2
  store float 1.0, ptr %tmp19
  %tmp20 = load %Vector3, ptr %tmp16
  %tmp21 = getelementptr %Transform, ptr %tmp3, i32 0, i32 2
  store %Vector3 %tmp20, ptr %tmp21
  %tmp22 = load %Transform, ptr %tmp3
  %tmp23 = getelementptr %Entity, ptr %entity_main, i32 0, i32 0
  store %Transform %tmp22, ptr %tmp23
  %tmp24 = getelementptr %Entity, ptr %entity_main, i32 0, i32 1
  store float 70.0, ptr %tmp24
  %tmp25 = getelementptr %Entity, ptr %entity_main, i32 0, i32 2
  store float 1.5, ptr %tmp25
  %tmp26 = getelementptr %Entity, ptr %entity_main, i32 0, i32 3
  store i1 0, ptr %tmp26

  %ii_main = alloca i32
  store i32 45, ptr %ii_main
  %jj_main = alloca i32
  store i32 4, ptr %jj_main
  %HP_main = alloca float
  %tmp27 = getelementptr inbounds %Entity, ptr %entity_main, i32 0, i32 1
  %tmp28 = load float, ptr %tmp27
  store float %tmp28, ptr %HP_main
  %healthPtr_main = alloca ptr
  store ptr %HP_main, ptr %healthPtr_main
  %tmp29 = load ptr, ptr %healthPtr_main
  store float 5.0, ptr %tmp29
  %moreHealth_main = alloca float
  %tmp30 = getelementptr inbounds %Entity, ptr %entity_main, i32 0, i32 1
  %tmp31 = load float, ptr %tmp30
  %tmp32 = fadd float 1.0, %tmp31

  store float %tmp32, ptr %moreHealth_main
  store ptr %moreHealth_main, ptr %healthPtr_main
  %tmp33 = load ptr, ptr %healthPtr_main
  store ptr %tmp33, ptr %voidPtr_main
  %tmp34 = load ptr, ptr %healthPtr_main
  %tmp35 = load float, ptr %tmp34
  store float %tmp35, ptr %HP_main
  %tmp36 = load float, ptr %moreHealth_main
  %tmp37 = fadd float %tmp36, 1.0

  store float %tmp37, ptr %moreHealth_main
  %tmp38 = call i32 @m(i32 7)
  %tmp39 = call i1 @n(i1 1)
  %tmp40 = call i1 @n(i1 0)
  %t_main = alloca i32
  %tmp41 = call i32 @m(i32 8)
  store i32 %tmp41, ptr %t_main
  %u_main = alloca i32
  %tmp42 = call i32 @m(i32 8)
  %tmp43 = add i32 1, %tmp42

  %tmp44 = add i32 %tmp43, 9

  store i32 %tmp44, ptr %u_main
  %defaultInt_main = alloca i32
  %defaultFloat_main = alloca float
  %defaultBool_main = alloca i1
  %tmp45 = getelementptr inbounds %Entity, ptr %entity_main, i32 0, i32 1
  store float 4.0, ptr %tmp45
  %default_s32_main = alloca i32
  %default_f32_main = alloca float
  %default_bool_main = alloca i1
  %quad_main = alloca fp128
  store fp128 0xL1C3, ptr %quad_main
  %i_main = alloca i32
  store i32 -1, ptr %i_main
  %tmp46 = load i32, ptr %i_main
  %tmp47 = call i32 @Abs(i32 %tmp46)
  %tmp48 = load i32, ptr %i_main
  %tmp49 = sub i32 0, %tmp48
  %tmp50 = call i32 @Abs(i32 %tmp49)
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
  %tmp51 = load i32, ptr %a_main
  %tmp52 = load i32, ptr %b_main
  %tmp53 = add i32 %tmp51, %tmp52

  store i32 %tmp53, ptr %c_main
  %d_main = alloca i32
  store i32 4, ptr %d_main
  %tmp54 = load i32, ptr %a_main
  %tmp55 = load i32, ptr %c_main
  %tmp56 = add i32 %tmp54, %tmp55

  store i32 %tmp56, ptr %d_main
  %tmp57 = load i32, ptr %d_main
  %tmp58 = add i32 %tmp57, 42

  store i32 %tmp58, ptr %d_main
  %f_main = alloca float
  store float 2.5, ptr %f_main
  %tmp59 = load float, ptr %f_main
  %tmp60 = fmul float %tmp59, 2.0

  store float %tmp60, ptr %f_main
  %g_main = alloca float
  store float 3.0, ptr %g_main
  %h_main = alloca float
  %tmp61 = load float, ptr %f_main
  %tmp62 = load float, ptr %g_main
  %tmp63 = fsub float %tmp61, %tmp62

  store float %tmp63, ptr %h_main
  %ok_main = alloca i1
  %tmp64 = load i32, ptr %c_main
  %tmp65 = icmp sgt i32 %tmp64, 5

  store i1 %tmp65, ptr %ok_main
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_main_str, i32 0, i32 0), i32 0)
  ret i32 0
}

@.fn_fn_str = private constant [3 x i8] c"fn\00"
define void @fn(i32 %i) {
entry:
  %tmp66 = alloca i32
  store i32 %i, ptr %tmp66
  %j_fn = alloca i32
  %tmp67 = load i32, ptr %tmp66
  %tmp68 = sdiv i32 %tmp67, 7

  store i32 %tmp68, ptr %j_fn
  ret void 
}

@.fn_Branchy_str = private constant [8 x i8] c"Branchy\00"
define i32 @Branchy(i32 %value) {
entry:
  %tmp69 = alloca i32
  store i32 %value, ptr %tmp69
  %tmp70 = load i32, ptr %tmp69
  %tmp71 = icmp eq i32 %tmp70, 0

  br i1 %tmp71, label %if_then_0, label %if_else_0
if_then_0:
  store i32 42, ptr %tmp69
  %f_if_then420 = alloca float
  store float 0.0, ptr %f_if_then420
  store float -1.0, ptr %f_if_then420
  br label %if_end_0
if_else_0:
  store i32 -42, ptr %tmp69
  %f_if_else436 = alloca float
  store float 78.0, ptr %f_if_else436
  br label %if_end_0
if_end_0:
  %tmp72 = load i32, ptr %tmp69
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_Branchy_str, i32 0, i32 0), i32 %tmp72)
  ret i32 %tmp72
}

@.fn_Abs_str = private constant [4 x i8] c"Abs\00"
define i32 @Abs(i32 %value) {
entry:
  %tmp73 = alloca i32
  store i32 %value, ptr %tmp73
  %tmp74 = load i32, ptr %tmp73
  %tmp75 = icmp slt i32 %tmp74, 0

  br i1 %tmp75, label %if_then_1, label %if_end_1
if_then_1:
  %tmp76 = load i32, ptr %tmp73
  %tmp77 = sub i32 0, %tmp76
  ret i32 %tmp77
  br label %if_end_1
if_end_1:
  %tmp78 = load i32, ptr %tmp73
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_Abs_str, i32 0, i32 0), i32 %tmp78)
  ret i32 %tmp78
}

@.fn_Loopy_str = private constant [6 x i8] c"Loopy\00"
define i32 @Loopy(i32 %value) {
entry:
  %tmp79 = alloca i32
  store i32 %value, ptr %tmp79
  %i_Loopy = alloca i32
  store i32 0, ptr %i_Loopy
  br label %for_cond2
for_cond2:
  %tmp80 = load i32, ptr %i_Loopy
  %tmp81 = load i32, ptr %tmp79
  %tmp82 = icmp slt i32 %tmp80, %tmp81

  br i1 %tmp82, label %for_body2, label %for_end2
for_body2:
  %tmp83 = load i32, ptr %tmp79
  %tmp84 = load i32, ptr %i_Loopy
  %tmp85 = add i32 %tmp83, %tmp84

  store i32 %tmp85, ptr %tmp79
  br label %for_iter2
for_iter2:
  %tmp86 = load i32, ptr %i_Loopy
  %tmp87 = sub i32 %tmp86, 3

  store i32 %tmp87, ptr %i_Loopy
  br label %for_cond2
for_end2:
  %tmp88 = load i32, ptr %tmp79
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_Loopy_str, i32 0, i32 0), i32 %tmp88)
  ret i32 %tmp88
}

@.fn_n_str = private constant [2 x i8] c"n\00"
define i1 @n(i1 %b) {
entry:
  %tmp89 = alloca i1
  store i1 %b, ptr %tmp89
  %tmp90 = load i1, ptr %tmp89
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([3 x i8], ptr @.fn_n_str, i32 0, i32 0), i1 %tmp90)
  ret i1 %tmp90
}

@.fn_m_str = private constant [2 x i8] c"m\00"
define i32 @m(i32 %j) {
entry:
  %tmp91 = alloca i32
  store i32 %j, ptr %tmp91
  %i_m = alloca i32
  store i32 3, ptr %i_m
  %tmp92 = load i32, ptr %tmp91
  %tmp93 = add i32 %tmp92, 4

  store i32 %tmp93, ptr %tmp91
  %health_m = alloca i32
  store i32 7, ptr %health_m
  %tmp94 = load i32, ptr %tmp91
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_m_str, i32 0, i32 0), i32 %tmp94)
  ret i32 %tmp94
}

@.fn_IsAlive_str = private constant [8 x i8] c"IsAlive\00"
define i1 @Entity.IsAlive(ptr %this) {
entry:
  %tmp95 = getelementptr %Entity, ptr %this, i32 0, i32 1
  %tmp96 = load float, ptr %tmp95
  %tmp97 = fcmp ogt float %tmp96, 0.0

  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([3 x i8], ptr @.fn_IsAlive_str, i32 0, i32 0), i1 %tmp97)
  ret i1 %tmp97
}

@.fn_AllocB_str = private constant [7 x i8] c"AllocB\00"
define ptr @AllocB(i64 %size) {
entry:
  %tmp98 = alloca i64
  store i64 %size, ptr %tmp98
  %tmp99 = load i64, ptr %tmp98
  %tmp100 = call ptr @__th_allocB(i64 %tmp99)
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_AllocB_str, i32 0, i32 0), ptr %tmp100)
  ret ptr %tmp100
}

@.fn_ReallocB_str = private constant [9 x i8] c"ReallocB\00"
define ptr @ReallocB(ptr %ptr, i64 %newSize) {
entry:
  %tmp101 = alloca ptr
  store ptr %ptr, ptr %tmp101
  %tmp102 = alloca i64
  store i64 %newSize, ptr %tmp102
  %tmp103 = load ptr, ptr %tmp101
  %tmp104 = load i64, ptr %tmp102
  %tmp105 = call ptr @__th_reallocB(ptr %tmp103, i64 %tmp104)
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_ReallocB_str, i32 0, i32 0), ptr %tmp105)
  ret ptr %tmp105
}

@.fn_Free_str = private constant [5 x i8] c"Free\00"
define void @Free(ptr %ptr) {
entry:
  %tmp106 = alloca ptr
  store ptr %ptr, ptr %tmp106
  %tmp107 = load ptr, ptr %tmp106
  call void @__th_free(ptr %tmp107)
  ret void 
}

