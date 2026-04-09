struct Holder {
    boxed: Box(i32),
    val: i32
}

fn main() -> i32 {
    {
        let h = make Holder {
            boxed: Box.New(100),
            val: 5
        };
    }
    42
}
