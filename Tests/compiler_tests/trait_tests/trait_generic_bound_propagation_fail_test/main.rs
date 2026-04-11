trait Foo {}
trait Bar {}
fn bar(T:! Sized +Foo + Bar) -> i32 { 0 }
fn idk(T:! Sized +Foo) -> i32 {
    bar(T)
}
fn main() -> i32 {
    0
}
