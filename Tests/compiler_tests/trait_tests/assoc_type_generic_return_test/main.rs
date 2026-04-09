trait Identity {
    let Out :! type;
    fn id(val: Self) -> Self.Out;
}

impl Identity for i32 {
    let Out :! type = Self;
    fn id(val: i32) -> (i32 as Identity).Out {
        val
    }
}

fn main() -> i32 {
    i32.id(42)
}
