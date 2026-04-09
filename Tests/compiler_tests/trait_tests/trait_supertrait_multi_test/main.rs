trait Foo {
    fn foo_val() -> i32;
}
trait Bar {
    fn bar_val() -> i32;
}
trait Both: Foo + Bar {
    fn both_val() -> i32;
}
fn needs_foo(T:! Foo) -> i32 {
    T.foo_val()
}
fn needs_bar(T:! Bar) -> i32 {
    T.bar_val()
}
fn needs_both(T:! Both) -> i32 {
    needs_foo(T) + needs_bar(T) + T.both_val()
}
impl Foo for i32 {
    fn foo_val() -> i32 { 1 }
}
impl Bar for i32 {
    fn bar_val() -> i32 { 2 }
}
impl Both for i32 {
    fn both_val() -> i32 { 3 }
}
fn main() -> i32 {
    needs_both(i32)
}
