// *GcMut.New(45) — the GcMut is a temporary. The deref reads the i32 (45),
// and the GcMut itself must be dropped (freed) at scope exit.
// We verify the value is read correctly; memory safety verified by running
// under a sanitizer. The program should exit with code 45.
fn main() -> i32 {
    *GcMut.New(45)
}
