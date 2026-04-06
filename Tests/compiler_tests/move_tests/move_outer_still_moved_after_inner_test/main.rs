struct Idk {
    val: i32
}

fn main() -> i32 {
    let a = make Idk { val : 4 };
    let b = a;
    {
        let c = make Idk { val : 10 };
    }
    a.val
}
