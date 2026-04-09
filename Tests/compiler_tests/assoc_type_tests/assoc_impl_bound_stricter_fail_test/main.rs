trait Producer: Sized {
    let Item :! MetaSized;
}

impl Producer for i32 {
    let Item :! type = i32;
}

fn main() -> i32 { 0 }
