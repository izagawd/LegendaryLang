trait Foo{
    fn kk(T:! type);
}
fn bruh(T:! Foo, U:! type){
    (T as Foo).kk(U);
    T.kk(U);
}
fn main() -> i32{
    4
}
