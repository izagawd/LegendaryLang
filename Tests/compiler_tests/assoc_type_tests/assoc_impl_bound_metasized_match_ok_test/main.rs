trait Producer: Sized {
    let Item :! MetaSized;
}

impl Producer for i32 {
    let Item :! MetaSized = str;
}

fn main() -> i32 { 0 }
