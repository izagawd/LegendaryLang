trait Bar(T:! Sized) {
    fn bar_val() -> i32;
}
trait Foo: Bar(i32) {
    fn foo_val() -> i32;
}
fn needs_bar_i32(T:! Sized +Bar(i32)) -> i32 {
    T.bar_val()
}
fn needs_foo(T:! Sized +Foo) -> i32 {
    needs_bar_i32(T) + T.foo_val()
}
impl Bar(i32) for i32 {
    fn bar_val() -> i32 { 7 }
}
impl Foo for i32 {
    fn foo_val() -> i32 { 8 }
}
fn main() -> i32 {
    needs_foo(i32)
}
