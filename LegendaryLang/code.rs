struct A{
    field : std::primitive::i32,

}
fn gay(kk : something::A) -> something::A {
    kk
}
struct B{
    nested: something::A,
    field2: std::primitive::i32,
}
fn main() -> std::primitive::i32{

    let a = gay(something::A{field = 5});
    a.field
}