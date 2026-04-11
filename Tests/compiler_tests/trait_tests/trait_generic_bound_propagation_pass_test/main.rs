trait Foo {}
trait Bar {}
fn bar(T:! Sized +Foo + Bar) -> i32 { 5 }
fn idk(T:! Sized +Foo + Bar) -> i32 {
    bar(T)
}
impl Foo for i32 {}
impl Bar for i32 {}
fn main() -> i32 {
    idk(i32)
}
