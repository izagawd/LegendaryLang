struct A{
    field : std::primitive::i32,

}
struct Nester{
    f: std::primitive::i32,
    dd: something::A
}
fn gay() -> something::Nester{
    something::Nester{f= 5, dd = something::A{field = 5}}
}
struct B{
    nested: something::A,
    field2: std::primitive::i32,
}
fn main() -> std::primitive::i32{

}