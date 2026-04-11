trait Foo {
    fn kk(T:! Sized) -> i32;
}
fn bruh(T:! Sized +Foo, U:! Sized) -> i32 {
    (T as Foo).kk(U)
}
impl Foo for i32 {
    fn kk(T:! Sized) -> i32 { 7 }
}
fn main() -> i32 {
    bruh(i32, i32)
}
