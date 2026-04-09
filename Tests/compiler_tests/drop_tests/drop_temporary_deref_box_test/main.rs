// *Box.New(45) — the Box is a temporary. The deref reads the i32 (45),
// and the Box itself must be dropped (freed) at scope exit.
// We verify the value is read correctly; memory safety verified by running
// under a sanitizer. The program should exit with code 45.
fn main() -> i32 {
    *Box.New(45)
}
