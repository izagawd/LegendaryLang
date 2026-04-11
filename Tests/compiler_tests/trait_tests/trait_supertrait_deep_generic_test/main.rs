trait A(T:! Sized) {
    fn a_val() -> i32;
}
trait B(T:! Sized): A(T) {
    fn b_val() -> i32;
}
trait C(T:! Sized): B(T) {
    fn c_val() -> i32;
}
fn needs_a(U:! Sized, T:! Sized +A(U)) -> i32 {
    T.a_val()
}
fn needs_c(U:! Sized, T:! Sized +C(U)) -> i32 {
    needs_a(U, T) + T.c_val()
}
impl A(i32) for i32 {
    fn a_val() -> i32 { 1 }
}
impl B(i32) for i32 {
    fn b_val() -> i32 { 2 }
}
impl C(i32) for i32 {
    fn c_val() -> i32 { 3 }
}
fn main() -> i32 {
    needs_c(i32, i32)
}
