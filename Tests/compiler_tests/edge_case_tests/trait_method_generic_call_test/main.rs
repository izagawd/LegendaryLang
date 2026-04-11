trait Foo{
    fn kk(T:! Sized);
}
fn bruh(T:! Sized +Foo, U:! Sized){
    (T as Foo).kk(U);
    T.kk(U);
}
fn main() -> i32{
    4
}
