struct Holder {
    boxed: GcMut(i32),
    val: i32
}

fn main() -> i32 {
    {
        let h = make Holder {
            boxed: GcMut.New(100),
            val: 5
        };
    }
    42
}
