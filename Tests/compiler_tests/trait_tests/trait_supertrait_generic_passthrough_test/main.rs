trait Bar(T:! type) {
    fn bar_val() -> i32;
}
trait Foo(T:! type): Bar(T) {
    fn foo_val() -> i32;
}
fn needs_bar(U:! type, T:! Bar(U)) -> i32 {
    T.bar_val()
}
fn needs_foo(U:! type, T:! Foo(U)) -> i32 {
    needs_bar(U, T) + T.foo_val()
}
impl Bar(i32) for i32 {
    fn bar_val() -> i32 { 10 }
}
impl Foo(i32) for i32 {
    fn foo_val() -> i32 { 5 }
}
fn main() -> i32 {
    needs_foo(i32, i32)
}
