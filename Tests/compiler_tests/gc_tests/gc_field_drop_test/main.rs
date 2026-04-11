struct Holder {
    boxed: Gc(i32),
    val: i32
}

fn main() -> i32 {
    {
        let h = make Holder {
            boxed: Gc.New(100),
            val: 5
        };
    }
    42
}
