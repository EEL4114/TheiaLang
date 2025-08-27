; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"

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

define internal void @theia.__th_free(ptr %user) nounwind {
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