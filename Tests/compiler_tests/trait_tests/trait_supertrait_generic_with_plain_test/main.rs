trait Bar {
    fn bar_val() -> i32;
}
trait Foo(T:! Sized): Bar {
    fn foo_val() -> i32;
}
fn needs_bar(T:! Sized +Bar) -> i32 {
    T.bar_val()
}
fn needs_foo(U:! Sized, T:! Sized +Foo(U)) -> i32 {
    needs_bar(T) + T.foo_val()
}
impl Bar for i32 {
    fn bar_val() -> i32 { 20 }
}
impl Foo(i32) for i32 {
    fn foo_val() -> i32 { 3 }
}
fn main() -> i32 {
    needs_foo(i32, i32)
}
