struct A{
    field : std::primitive::i32

}

struct B{
    nested: something::A
}
fn main() -> std::primitive::i32{
    let a = something::B{
        nested = something::A{
        field = 5
    }
    };
    let kk = 11;
    let madeA = ((something::A{
        field = ((100))
    }));
    a.nested = madeA;

    a.nested.field
}