struct Idk {
    val: i32
}

fn consume(x: Idk) -> i32 {
    x.val
}

fn main() -> i32 {
    let a = make Idk { val : 4 };
    {
        let a = make Idk { val : 99 };
        let b = a;
    }
    a.val
}
