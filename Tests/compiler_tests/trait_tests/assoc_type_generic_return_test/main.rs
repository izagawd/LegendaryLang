trait Identity {
    type Out;
    fn id(val: Self) -> Self.Out;
}

impl Identity for i32 {
    type Out = Self;
    fn id(val: i32) -> (i32 as Identity).Out {
        val
    }
}

fn main() -> i32 {
    i32.id(42)
}
