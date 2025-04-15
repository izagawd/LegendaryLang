struct A{
    field : std::primitive::i32,

}

struct B{
    nested: something::A,
    field2: std::primitive::i32,
}
fn main() -> std::primitive::i32{
    let a = something::B{
        nested = something::A{
        field = 5
    },
field2 = 4};



    a.field2
}