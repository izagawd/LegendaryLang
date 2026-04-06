trait Addable {
    fn Val() -> i32;
}
impl Addable for i32 {
    fn Val() -> i32 { 1 }
}
impl Addable for bool {
    fn Val() -> i32 { 2 }
}
fn interleaved(A:! Addable, x: i32, B:! Addable, y: i32) -> i32 {
    A.Val() + x + B.Val() + y
}
fn main() -> i32 {
    interleaved(i32, 10, bool, 20)
}
