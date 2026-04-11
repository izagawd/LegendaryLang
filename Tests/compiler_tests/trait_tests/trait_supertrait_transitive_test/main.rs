trait A {
    fn a_val() -> i32;
}
trait B: A {
    fn b_val() -> i32;
}
trait C: B {
    fn c_val() -> i32;
}
fn needs_a(T:! Sized +A) -> i32 {
    T.a_val()
}
fn needs_c(T:! Sized +C) -> i32 {
    needs_a(T) + T.c_val()
}
impl A for i32 {
    fn a_val() -> i32 { 1 }
}
impl B for i32 {
    fn b_val() -> i32 { 2 }
}
impl C for i32 {
    fn c_val() -> i32 { 3 }
}
fn main() -> i32 {
    needs_c(i32)
}
