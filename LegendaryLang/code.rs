struct A{
    field : std::primitive::i32,

}
struct Nester{
    f: std::primitive::i32,
    dd: something::A
}
fn gay(kk : something::A) ->  something::A {
    kk
}
struct B{
    nested: something::A,
    field2: std::primitive::i32,
}
fn main() -> std::primitive::i32{
   let gayed =  something::A{field = 5};
    gayed.field = 10;
    gayed.field
}