trait Foo {
    fn kk(T:! Sized) -> i32;
}
fn bruh(T:! Sized +Foo, U:! Sized) -> i32 {
    (T as Foo).kk(U);
    T.kk(U)
}
fn main() -> i32 {
    4
}
