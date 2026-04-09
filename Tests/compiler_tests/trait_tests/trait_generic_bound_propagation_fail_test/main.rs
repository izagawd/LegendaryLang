trait Foo {}
trait Bar {}
fn bar(T:! Foo + Bar) -> i32 { 0 }
fn idk(T:! Foo) -> i32 {
    bar(T)
}
fn main() -> i32 {
    0
}
